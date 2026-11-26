# Architecture

Grootscape develops the Rust client and C# server in the same repository. They build independently and meet at a versioned network contract.

| Component | Location | Responsibility |
|---|---|---|
| Rust client | `src/grootscape-client` | Run the first-party client. The engine will own rendering, game UI, input mapping, asset loading, and presentation of server state. |
| C# server | `src/Grootscape.Server` | Host the authoritative world. The server will validate commands and own simulation, player state, and persistence. |
| Integration tests | `tests/Grootscape.Server.Tests` | Run the ASP.NET Core host in process and verify externally visible behavior. |

## Platforms

The native apps share one Rust game engine and in-game UI. The first renderer milestone uses `wgpu` and tests an original scene on Apple Silicon and Android. Platform hosts provide a surface, input, lifecycle events, and system services.

| Platform | Direction |
|---|---|
| macOS | Swift/Xcode app integrating the Rust engine. |
| Windows and Linux | Shared Rust desktop host. |
| Android | Kotlin/Android Studio app integrating the Rust engine. |
| Browser | TypeScript client derived from TSPS under its applicable licenses. |
| iOS | Later milestone using the shared Rust engine. |

## World and protocol

The server owns mutable world state. Give each world one writer, apply commands in a defined order on the simulation tick, and keep socket and database I/O outside that tick. Clients can interpolate presentation between updates. The first gameplay slice should document tick ordering and verify the same outcomes from a repeatable command sequence.

Define the wire contract with the first feature that uses it. Maintain shared request and response fixtures so Rust, C#, and TypeScript can verify compatibility independently. When browser interoperability depends on TSPS, record the exact upstream revision and the behavior verified against it.

## Content and client features

Use [TSPS](https://github.com/RSPSApp/tsps) as a behavior reference and as the starting point for the browser client. Keep upstream license notices with reused components. Use original or appropriately licensed assets for standalone engine and server fixtures.

Build selected RuneLite features into Grootscape. Port behavior feature by feature and keep platform hosts independent of the feature logic. Tile Indicators is the first candidate because it exercises tile picking, overlays, and the distinction between an intended destination and a server-confirmed position.
