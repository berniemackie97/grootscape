# Grootscape

Grootscape is a self-hosted multiplayer RPG. The first-party client and game engine are written in Rust. An authoritative C# server runs on .NET 10. The MVP targets macOS, Windows, Linux, Android, and the browser. iOS follows the MVP.

This checkout contains the Rust client entry point and ASP.NET Core server host. The client reports its version and the server exposes a liveness endpoint. The [architecture](docs/architecture.md) describes responsibilities and platform boundaries. Planned work is tracked in [GitHub Issues](https://github.com/berniemackie97/grootscape/issues).

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

GitHub Actions runs both toolchains on Linux, macOS, and Windows. Commit the affected lockfile with any dependency change. See [AGENTS.md](AGENTS.md) for the contribution rules.
