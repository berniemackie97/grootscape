# Architecture

Grootscape has one world server, one shared client engine, and platform-specific launch targets. The server owns the world. Clients send player intent and present authoritative state. Each platform host owns its window or surface, input, and app lifecycle.

| Build target | Source | Responsibility |
|---|---|---|
| `Grootscape.Server` | `src/Grootscape.Server` | C# world server and ASP.NET Core entry point. |
| `grootscape-engine` | `src/grootscape-engine` | Rust library shared by the native clients. It supplies the build version today. Rendering, assets, input mapping, and presentation belong here as they land. |
| `grootscape-client` | `src/grootscape-client` | Rust desktop executable. Its current entry point verifies that the engine links and reports its version. |
| `GrootscapeMac` | `apps/macos` | Swift app that links the Rust engine and displays its version in a window. |

## Server

Keep a single deployable server and organize features in folders inside it. Put tick progression, movement, collision, and other game rules in `World`. Put session lifecycle and packet handling in `Network`. Put real database adapters and migrations in `Persistence` when the first save ships. `Program.cs` wires the host together.

The world loop owns mutation. Network handlers deliver player intent to the loop, which applies commands in a defined order. Network and disk I/O run outside the tick. Keep the rules callable without ASP.NET, sockets, or a database so tests can drive them with explicit tick inputs. Existing host-level tests live in `tests/Grootscape.Server.Tests`.

## Client

Keep shared gameplay presentation and the `wgpu` renderer in the engine. The Rust desktop executable will host Windows and Linux. The macOS app already links the same engine through a small C interface. The Android Kotlin host will link the engine and own its surface lifecycle. The browser client is a TypeScript target that starts from TSPS. iOS follows the MVP.

The first windowed renderer and tile-picking work is tracked in [#1](https://github.com/berniemackie97/grootscape/issues/1). Android and browser integration are tracked in [#3](https://github.com/berniemackie97/grootscape/issues/3) and [#4](https://github.com/berniemackie97/grootscape/issues/4).

## Wire and content

The client and server meet at a versioned wire contract. Define its first messages and language-neutral fixtures with [the movement slice](https://github.com/berniemackie97/grootscape/issues/2). Codecs translate at each runtime's boundary. Keep world objects, persistence records, and graphics types outside the wire format.

Pin the TSPS revision for browser compatibility work. Preserve license notices for reused components. Use original or appropriately licensed assets in engine and server fixtures. Put shared in-game overlays in the engine, starting with [Tile Indicators](https://github.com/berniemackie97/grootscape/issues/5).
