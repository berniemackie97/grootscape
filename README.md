# Grootscape

Grootscape is a self-hosted multiplayer RPG. The first-party client and game engine are written in Rust. An authoritative C# server runs on .NET 10. The MVP targets macOS, Windows, Linux, Android, and the browser. iOS follows the MVP.

The current foundation builds the C# server, shared Rust engine, Rust desktop executable, and Swift macOS app. The desktop executable and macOS window report the engine version. The server exposes a liveness endpoint. The [architecture](docs/architecture.md) maps each build target to its source and defines the runtime boundaries. Planned work is tracked in [GitHub Issues](https://github.com/berniemackie97/grootscape/issues).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The minimum is `10.0.403`. CI uses `10.0.404`.
- [Rustup](https://rustup.rs/). The repository pins Rust, rustfmt, and Clippy in `rust-toolchain.toml`. A matching system installation works as well.

## Run locally

```sh
cargo run --locked --package grootscape-client -- --version
dotnet restore Grootscape.slnx --locked-mode
dotnet run --project src/Grootscape.Server --no-restore -- --urls http://127.0.0.1:5080
```

In a second terminal, run `curl --fail http://127.0.0.1:5080/health/live`. The host returns `Healthy`.

### macOS app

With Xcode or the Command Line Tools installed, build the engine and run the Swift app from the repository root.

```sh
cargo build --locked --release --target-dir target --package grootscape-engine
swift run --package-path apps/macos GrootscapeMac
```

Open `apps/macos/Package.swift` in Xcode to work on the macOS app after building the engine. Open `Grootscape.slnx` for the server and the repository root for the Cargo workspace.

## Verify a change

```sh
cargo fmt --all --check
cargo clippy --workspace --all-targets --locked -- -D warnings
cargo test --workspace --locked
cargo run --locked --package grootscape-client -- --version

dotnet restore Grootscape.slnx --locked-mode
dotnet format Grootscape.slnx --verify-no-changes --no-restore
dotnet build Grootscape.slnx --configuration Release --no-restore
dotnet test --solution Grootscape.slnx --configuration Release --no-build
```

GitHub Actions runs the Rust and .NET checks on Linux, macOS, and Windows. The macOS job also builds the Swift app and checks the engine version through the C interface. Commit the affected lockfile with any dependency change. See [AGENTS.md](AGENTS.md) for the contribution rules.
