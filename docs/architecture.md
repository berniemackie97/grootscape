# Architecture

Grootscape has one world server, one shared client engine, and a thin host per platform. The server owns the world. Clients send player intent and present authoritative state. Each host owns its window or surface, input, and app lifecycle, and forwards them to the engine.

```
apps/            platform hosts (Swift, Kotlin, browser page)
deploy/          container build for the server
docs/            architecture and decisions
protocol/        wire contract: specification and shared fixtures
src/client/      Rust crates (engine, protocol codec, hosts, FFI)
src/server/      C# projects (host, world rules, protocol codec)
tests/server/    C# test projects, one per server project
```

## Build targets

| Target | Source | Responsibility |
|---|---|---|
| `Grootscape.Server` | `src/server/Grootscape.Server` | The only deployable server. ASP.NET Core host that wires features together: `World/` runs the tick loop, `Network/` runs client sessions, `Health/` serves probes, `Observability/` configures logs, metrics, and traces. |
| `Grootscape.World` | `src/server/Grootscape.World` | Game rules and world state. No references to ASP.NET, sockets, or storage. |
| `Grootscape.Protocol` | `src/server/Grootscape.Protocol` | Server side of the wire codec. |
| `grootscape-engine` | `src/client/grootscape-engine` | Shared client engine: the `wgpu` renderer today; assets, input mapping, scene, and overlays as they land. |
| `grootscape-protocol` | `src/client/grootscape-protocol` | Client side of the wire codec. No dependencies. |
| `grootscape-desktop` | `src/client/grootscape-desktop` | `grootscape-client` executable for Windows, Linux, and macOS. Owns the `winit` window and event loop. |
| `grootscape-web` | `src/client/grootscape-web` | WebAssembly bindings the browser host calls. |
| `grootscape-native` | `src/client/grootscape-native` | C interface (`include/grootscape.h`) for Apple hosts and JNI methods for Android. |
| `GrootscapeMac` | `apps/macos` | Swift app that links `grootscape-native` statically. |
| Android app | `apps/android` | Kotlin app that loads `libgrootscape_native.so`, built per ABI by `cargo-ndk` from Gradle. |
| Browser page | `apps/web` | Static page that sizes the canvas, drives the frame loop, and loads `grootscape-web`. |

Dependencies point inward. Hosts depend on the engine; the engine depends on no host. The server host depends on `Grootscape.World` and `Grootscape.Protocol`, which depend on nothing in the solution.

## Server

`Program.cs` only wires features. Each feature folder registers its own services, options, and health checks through one extension method.

The world loop is the single writer of world state. It runs as a hosted service on a `PeriodicTimer` driven by the injected `TimeProvider`, so tests control time. A tick that runs long is logged and counted, and the timer coalesces missed ticks instead of running them back to back. Network handlers will hand player intent to the loop through a queue that the loop drains at the start of each tick; socket and disk I/O stay outside the tick.

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

## Wire protocol

All clients, including the browser, connect with a WebSocket to `/play`. [`protocol/README.md`](../protocol/README.md) specifies the messages and close codes, and `protocol/fixtures/v1.json` holds the exact bytes both codecs test against. Version 1 is the handshake; movement arrives with [#2](https://github.com/berniemackie97/grootscape/issues/2). Codecs translate at each runtime's boundary, so world objects, persistence records, and graphics types stay out of the wire format.

## Client

The renderer opens a `wgpu` device for whatever surface the host provides: Metal on macOS, Vulkan or DirectX 12 on Windows, Vulkan or GL on Linux, and WebGPU with a WebGL 2 fallback in the browser. When no hardware adapter is available, as in virtual machines, remote desktops, and CI, the renderer falls back to a software adapter (WARP on Windows, lavapipe on Linux). Hosts report the chosen adapter and backend at startup. The Android host links the same engine through JNI; rendering into its `SurfaceView` follows with [#3](https://github.com/berniemackie97/grootscape/issues/3). Scene rendering and tile picking follow with [#1](https://github.com/berniemackie97/grootscape/issues/1), and the first overlay with [#5](https://github.com/berniemackie97/grootscape/issues/5). iOS follows the MVP and will reuse `grootscape-native`.

TSPS is a behavior reference. Pin the upstream revision for any compatibility claim, preserve license notices for reused components, and use original or appropriately licensed assets in the repository and test fixtures.
