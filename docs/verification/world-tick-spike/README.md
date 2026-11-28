# World tick and intent spike (#23)

These sources record the temporary experiments. They are not part of the normal regression suite. They do not exercise the hosted world loop or a production intent queue. Issue #27 owns production scheduling and intake tests through the server's actual entry point.

The intake measurements use one thread for both writes and reads. They compare only sequential queue operation costs, not concurrent intake performance. The saturation test uses parallel write attempts, not simultaneous readers and writers. The workload omits the common admission gate, allocations, validation and network publication. Do not use the timings to rank concurrent intake strategies. See #23 and the world tick ADR in #173 for the results and limits.

## Repeat the experiments

Use the pinned .NET SDK. Run these commands from a checkout of the commit that contains this directory. The temporary worktree keeps the normal suite and your current changes intact.

```sh
worktree=$(mktemp -d)
git worktree add --detach "$worktree" HEAD
(
    cd "$worktree" || exit 1
    cp docs/verification/world-tick-spike/IntentQueueTests.cs tests/server/Grootscape.Server.Tests/
    cp docs/verification/world-tick-spike/TickSchedulingTests.cs tests/server/Grootscape.Server.Tests/
    dotnet test --project tests/server/Grootscape.Server.Tests/Grootscape.Server.Tests.csproj --configuration Release --output Detailed
)
```

Record the result before removing the temporary worktree:

```sh
git worktree remove --force "$worktree"
```

The original run on SDK 10.0.403 arm64 passed 33 tests (20 prototype cases plus 13 existing server cases). Only the three sequential measurement method names, output labels, and research comments changed when the sources moved here. Timing varies between runs.
