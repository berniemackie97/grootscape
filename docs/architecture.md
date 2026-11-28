# Architecture

Grootscape has one world server, one shared client engine, and a thin host per platform. The server owns the world. Clients send player intent and present authoritative state. Each host owns its window or surface, input, and app lifecycle, and forwards them to the engine.

```
apps/            platform hosts (Swift, Kotlin, browser page)
deploy/          container build for the server
docs/            architecture and decisions
protocol/        wire contract: specification and shared fixtures
src/client/      C++23 sources and public headers
src/server/      C# projects (host, world rules, protocol codec)
tests/server/    C# test projects, one per server project
```

## Build targets

| Target | Source | Responsibility |
|---|---|---|
| `Grootscape.Server` | `src/server/Grootscape.Server` | The only deployable server. ASP.NET Core host that wires features together: `World/` runs the tick loop, `Network/` runs client sessions, `Health/` serves probes, `Observability/` configures logs, metrics, and traces. |
| `Grootscape.World` | `src/server/Grootscape.World` | Game rules and world state. No references to ASP.NET, sockets, or storage. |
| `Grootscape.Protocol` | `src/server/Grootscape.Protocol` | Server side of the wire codec. |
| `grootscape_engine` | `src/client/cpp/renderer.cpp` | Shared client engine: the SDL3 renderer today; assets, input mapping, scene, and overlays as they land. |
| `grootscape_protocol` | `src/client/cpp/protocol.cpp` | Client side of the wire codec. No dependencies. |
| `grootscape-client` | `src/client/cpp/desktop.cpp` | `grootscape-client` executable for Windows, Linux, and macOS. Owns the SDL3 window and event loop. |
| `grootscape_web` | `src/client/cpp/web.cpp` | WebAssembly bindings the browser host calls. |
| `grootscape_native` | `src/client/cpp/native.cpp` | C interface (`include/grootscape/native.h`) for Apple hosts and JNI methods for Android. |
| `GrootscapeMac` | `apps/macos` | Swift app that links `grootscape_native` statically. |
| Android app | `apps/android` | Kotlin app that loads `libgrootscape_native.so`, built per ABI by CMake from Gradle. |
| Browser page | `apps/web` | Static page that sizes the canvas, drives the frame loop, and loads `grootscape_web`. |

Dependencies point inward. Hosts depend on the engine; the engine depends on no host. The server host depends on `Grootscape.World` and `Grootscape.Protocol`, which depend on nothing in the solution.

## Server

`Program.cs` only wires features. Each feature folder registers its own services, options, and health checks through one extension method.

The world loop is the single writer of world state. It runs as a hosted service on a `PeriodicTimer` driven by the injected `TimeProvider`, so tests control time. A tick that runs long is logged and counted, and the timer coalesces missed ticks instead of running them back to back. Network handlers will hand player intent to the loop through a queue that the loop drains at the start of each tick; socket and disk I/O stay outside the tick.

The proposed [world tick and intent decision](decisions/0002-world-tick-and-intent-processing.md) defines the next simulation slice. It does not describe behavior implemented in the current host.

Configuration is bound to options classes and validated at startup, so a bad value stops the process before it serves traffic. `appsettings.json` holds the defaults:

| Section | Setting | Default |
|---|---|---|
| `World` | `TickDurationMs` | 600 |
| `Network` | `HandshakeTimeoutMs`, `CloseTimeoutMs` | 5000, 2000 |
| `Network` | `KeepAliveIntervalSeconds` | 30 |
| `Network` | `AllowedOrigins` | empty (any origin) |

Production logs are JSON on stdout. OpenTelemetry collects ASP.NET Core, runtime, and `Grootscape.Server` metrics (`grootscape.world.*`, `grootscape.network.*`) and request traces; setting the standard `OTEL_EXPORTER_OTLP_ENDPOINT` exports logs, metrics, and traces over OTLP.

`/health/live` answers while the process runs. `/health/ready` answers once the world has ticked and stays healthy while it keeps ticking. On shutdown the server closes open sessions with status 1001 and stops the world loop.

`deploy/server.Dockerfile` publishes the server onto the chiseled ASP.NET image, which has no shell and runs as a non-root user on port 8080.

## World content

The proposed [world region decision](decisions/0003-world-region-format.md) defines the source and binary content contracts for the offline compiler and the two runtime loaders. It does not describe implemented loading behavior. The compiler publishes new build directories; #54 owns authenticated package delivery and activation.

## Wire protocol

All clients, including the browser, connect with a WebSocket to `/play`. [`protocol/README.md`](../protocol/README.md) specifies the messages and close codes, and `protocol/fixtures/v1.json` holds the exact bytes both codecs test against. Version 1 is the handshake; movement arrives with [#2](https://github.com/berniemackie97/grootscape/issues/2). Codecs translate at each runtime's boundary, so world objects, persistence records, and graphics types stay out of the wire format.

## Client

CMake builds C++23 client targets. `grootscape_protocol` owns the dependency-free client wire codec. `grootscape_native` supplies the static-lifetime C version string and Android JNI version method. `grootscape_engine` owns an SDL renderer; its host owns the window and must outlive the renderer. All graphics calls and destruction run on the host thread. No mutable game state exists in the client foundation.

Desktop owns SDL initialization, window, event loop and CLI. Destruction runs in reverse order: renderer, window, SDL. Zero-sized or minimized surfaces skip rendering. SDL handles resize and device reset internally; failed rendering stops the host with an error. Browser exports are narrow C entry points for the page lifecycle. The page owns canvas layout, physical pixel sizing and requestAnimationFrame; it avoids starting a zero-sized canvas, stops on context loss and frees the native session on pagehide. Reload restarts a lost WebGL context. Swift and Android retain their version-only host behavior; #45 and #43 own future rendering integration.

The [backend decision](decisions/0001-cpp23-client.md) records SDL3 rendering and WebGL 2 in place of wgpu. Platform smoke tests prove a clear frame, not future 3D capabilities. There is no client networking session yet (#36); integration tests connect the C++ codec to the actual .NET endpoint without adding a production transport prematurely.

TSPS is a behavior reference. Pin upstream revisions for compatibility claims, preserve component license notices and use original or appropriately licensed assets.
