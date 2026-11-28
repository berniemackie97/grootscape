# 0002: World tick and intent processing

Status: proposed; owner review required.

## Context

At main commit `d5e28e948f1f07e6eebefffdc96f85aeb6d53ee1`, the hosted world loop uses a `TimeProvider` and a `PeriodicTimer`. It increments the world tick but does not accept gameplay intent. Issue #23 defines the decision for #6 and its movement stories. This decision does not implement gameplay.

TSPS processes scheduled tasks, ground items, lifecycle changes, NPC mechanics and complete player turns in that order. It processes queued gameplay input at the start of each player turn. It then completes remaining mechanics, builds visibility updates, flushes network output and resets update flags. It changes player priority initially and every 100–150 ticks with Fisher–Yates, except that players in duels retain their slots. Some network messages bypass its turn queue. These are observations of the pinned reference, not proof of OSRS behavior.

## Decision

### Ownership and input boundary

Use a bounded FIFO queue for each authenticated world session. The host decodes and validates message structure and submits immutable intent values. Only the world loop validates gameplay rules and changes mutable world state. Preserve decoded message order within each session; the network receive path submits one session's messages in order. Do not use network arrival order to set priority between players.

Use a synchronized `Queue<T>` with `Lock` for each session. Use a common admission gate to establish one cutoff for the whole world, including session registration and the admission state. The gate orders admissions relative to the cutoff, not the time a packet arrived at a socket. All admissions and lifecycle notifications acquire the gate before the session lock. At the start of each tick, the world loop acquires the gate and captures the eligible count for each session and the pending lifecycle notifications. Release the gate before executing any intent. Only the world loop removes queued intents, so it can drain exactly the captured count under the session lock, releasing the lock before applying each intent. New arrivals remain behind the captured items and are not eligible until the next cutoff. Do not acquire the admission gate while holding a session lock. No network I/O, gameplay execution or asynchronous wait occurs under either lock. The gate and capture add cost not included in the prototype; measure them in #27 and #50.

Apply session lifecycle notifications captured at the cutoff in their admission order. World-owned NPC lifecycle changes follow the world phase order. Sessions that join at the cutoff can take a turn, but their gameplay input is eligible only after world admission. For M1, discard pending input and remove the player at the first cutoff that observes a disconnect; the grace period is zero ticks. A disconnect after the cutoff takes effect at the next cutoff, not during a player's turn. Prevent further admissions for closed sessions. Do not reuse a session identity for a new connection. TSPS can execute already queued input for a disconnected player held in combat; this decision deliberately retains the discard requirement in #27 for M1. Review combat retention and its input policy in #105 and #116 before implementing that behavior.

Use 240 pending intents per session as the initial configurable limit, based on the TSPS reference. Validate the limit at startup. On overflow, reject the new intent, stop admission for that session, record the reason and schedule close status 1008 outside the tick. Do not silently overwrite or drop accepted input to make space. The disconnect rule determines when pending input is discarded. A full queue must not prevent admission to another session. Bound the number of intents eligible for each turn by the queue limit. Review limits with load evidence; the prototype does not establish a production limit.

### Tick phases

1. Advance the world tick once and capture the common input and lifecycle cutoff.
2. Process scheduled tasks and ground item timers.
3. Apply captured player and NPC lifecycle changes.
4. Process NPC mechanics.
5. Determine player priority and complete each eligible player turn in that order. Within a turn, validate and apply the captured FIFO input, apply due combat hits and timers, process pre-movement combat, pursuit and movement, post-movement combat, walk-to interactions and remaining player mechanics.
6. Process remaining world interactions and hits, then rebuild visibility data.
7. Build authoritative updates from the resulting state and submit them to outbound queues.
8. Reset transient update flags and release identifiers only after all observers have their updates.

Implement only the phases with actual behavior in each story. Do not create empty subsystems for future mechanics. Gameplay input admitted after the cutoff of tick N is eligible at the cutoff of tick N+1, not partway through another player's turn. This differs from the per-player input cutoff in TSPS and preserves the fairness requirement in #27.

### Player priority and replay

Use a world-owned order independent of network arrival time. Randomize the initial order and shuffle with Fisher–Yates at intervals of 100–150 simulation ticks, inclusive. Remove departed players; append new players in the captured lifecycle order. Once duels exist, preserve the slots of players in active duels during a shuffle. Do not implement duel state in M1.

Use a controlled source of random decisions. Record the initial state, tick numbers, session identities, ordered lifecycle events, ordered eligible intents with payloads and per-session sequence, player priority and random decisions (or reproducible random state with algorithm identity), and external clock values that affect simulation. Replay advances ticks directly without sockets or real-time delays. Compare authoritative state and client observations. A seed alone is not sufficient if the algorithm or consumption order changes. The replay harness belongs to #32.

### Scheduling and publication

Use monotonic elapsed time from the injected `TimeProvider`. Wait until the first deadline (one tick interval after start). Anchor the next deadline to the actual first tick start plus one interval. On subsequent ticks, advance the previous deadline by one interval. If the actual start is strictly more than one interval after that advanced deadline, set the next deadline to actual start plus one interval. Otherwise retain the advanced deadline. Wait only for a positive remaining duration; if overdue, start the next tick without an additional delay. Check cancellation before each tick. Execute one complete tick at a time; do not increment the tick counter for an unexecuted tick. Record execution duration, overrun and start lateness. The default remains 600 ms.

The world loop builds updates after simulation. A bounded per-session outbound queue transfers immutable updates to a send loop outside simulation. Publication never awaits socket or disk I/O. If an outbound update cannot be admitted, do not silently drop ordered state or wait for space; mark the session unable to receive a complete stream, stop further admissions and initiate disconnect with status 1008 outside the tick. A stalled send also has a configured timeout; close with status 1008 on expiry. Record both reasons. Issue #33 defines and verifies the slow-consumer policy. Reset shared update flags only after all eligible observers have had their publication attempt. Do not release identifiers prematurely.

## Alternatives and consequences

A shared FIFO channel uses fewer queue instances but no independent session capacity and requires additional accounting and ordering to recover fair player turns. A per-session bounded channel provides useful asynchronous APIs, but the spike uses nonblocking admission and a synchronous consumer. The synchronized queue is direct and supports exact batch boundaries. The common gate adds contention; its cost and fairness under sustained overload remain unmeasured. Keep the implementation simple and change it only if evidence shows a problem.

The proposed scheduling policy retains small overruns and resets the deadline after severe lateness. It differs from the current periodic timer. It does not make a late world run faster than the available CPU or guarantee network latency.

## Verification evidence

The developer ran the temporary scheduler and intent queue tests locally on .NET SDK 10.0.403, arm64, Release: 33 passed, zero failed and zero skipped. The prototype is local and not part of this documentation change.

The sequential intake experiment created 1,000 sessions, used 100 warm-up ticks and 1,000 measured ticks, and wrote then read one integer intent per session per tick. The per-session capacity was 240; the shared channel capacity was 240,000. Both channels used `SingleReader = true`, `SingleWriter = false`, `FullMode = Wait`, and `AllowSynchronousContinuations = false` with nonblocking TryWrite/TryRead. The synchronized queue used a capacity check and FIFO operations under a `Lock`. Each read verified the expected value. A Stopwatch measured only the measured tick loops, excluding setup and warm-up.

| Candidate | Total (ms) | Mean per tick (ms) |
| --- | ---: | ---: |
| Per-session synchronized queue | 15.046 | 0.015046 |
| Shared bounded channel | 17.638 | 0.017638 |
| Per-session bounded channel | 34.087 | 0.034087 |

Earlier per-session channel measurements were 100.910 ms and 32.463 ms total. These results vary with the test environment and are not a throughput guarantee. The saturation test attempted 1,000 parallel writes into a capacity-240 synchronized queue and verified the accepted count, consumed count, value range and uniqueness. It did not compare the exact accepted and consumed sets or guarantee actual contention. The scheduler tests cover the first deadline, overdue execution, deadline calculation including the strict reset threshold, remaining waits and cancellation.

Reproduce the local prototype run while its test files are available:

```sh
dotnet test --project tests/server/Grootscape.Server.Tests/Grootscape.Server.Tests.csproj --configuration Release --output Detailed
```

The measurements omit simultaneous reads and writes, the common admission gate, allocations, realistic payloads, validation, gameplay, and network publication. #27 must test cutoff, overflow, lifecycle and concurrent correctness; #50 must measure representative end-to-end load. This evidence does not establish production performance or OSRS compatibility.

## Sources

- [TSPS scheduler](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/game/GameEngine.ts)
- [TSPS world phases](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/game/World.ts#L1271)
- [TSPS player priority](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/game/World.ts#L1043-L1077)
- [TSPS player turn](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/game/entity/impl/player/Player.ts#L545-L639)
- [TSPS input collection](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/net/NetworkBuilder.ts#L685-L742) and [queue constants](https://github.com/RSPSApp/tsps/blob/7c49b9e31e34ee4b50258fb438fea4544553d399/server/src/main/typescript/elvarg/net/NetworkConstants.ts)
- [OCS synchronized partition queue](https://github.com/berniemackie97/OpenConquerServer/blob/a462d8016886f1f1d37560ef8303caa1a7bb3ff4/src/OpenConquer.GameServer/World/Execution/WorldPartition.cs): engineering precedent, not a gameplay reference.

Local research MCP was unavailable. No independent OSRS evidence was retrieved for the ordering rules. The common cutoff, M1 disconnect policy and synchronization design are Grootscape decisions. No reference code or data was imported.

## Follow-up

- #27: implement scheduler, input ownership, cutoff, overload, priority and M1 session lifecycle; verify concurrent boundary behavior.
- #29: preserve per-player movement order and emit position changes for publication.
- #32: implement deterministic replay and the two-client regression.
- #33: implement nonblocking publication and slow-consumer handling.
- #50: measure actual load, including contention and allocation.
- #105 and #116: review combat-specific disconnect retention before adding it.
