# Grootscape

Grootscape is a self-hosted multiplayer RPG. An authoritative C# server runs on .NET 10. The client engine and renderer are written in Rust and run on macOS, Windows, Linux, Android, and the browser. iOS follows the MVP.

The foundation runs end to end: the server ticks the world, accepts WebSocket clients through a versioned handshake, and exposes health probes and OpenTelemetry. The desktop and browser clients open a `wgpu` surface and render, and the macOS and Android apps link the same engine. The [architecture](docs/architecture.md) maps every build target to its source. Planned work is tracked in [GitHub Issues](https://github.com/berniemackie97/grootscape/issues).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), `10.0.403` or later. CI uses `10.0.404`.
- [Rustup](https://rustup.rs/). `rust-toolchain.toml` pins Rust, rustfmt, and Clippy.
- For the browser client: `rustup target add wasm32-unknown-unknown` and `cargo install wasm-bindgen-cli --version 0.2.130 --locked`.
- For the Android app: Android Studio or the Android SDK with an NDK, JDK 17, `rustup target add aarch64-linux-android x86_64-linux-android`, and `cargo install cargo-ndk --version 4.1.2 --locked`.
- For the macOS app: Xcode or the Command Line Tools.
- For the server image: Docker.

## Run locally

### Server

```sh
dotnet restore Grootscape.slnx --locked-mode
dotnet run --project src/server/Grootscape.Server --no-restore -- --urls http://127.0.0.1:5080
```

`curl http://127.0.0.1:5080/health/ready` returns `Healthy` once the world has ticked. Clients connect to `ws://127.0.0.1:5080/play`. To run the container instead:

```sh
docker build --file deploy/server.Dockerfile --tag grootscape-server .
docker run --rm --publish 8080:8080 grootscape-server
```

### Desktop client (Windows, Linux, macOS)

```sh
cargo run --locked --package grootscape-desktop
```

The log names the GPU and backend. `--exit-after-frames <count>` closes the window after that many frames, for smoke tests.

### Browser client

```sh
cargo build --locked --release --package grootscape-web --target wasm32-unknown-unknown
wasm-bindgen --target web --no-typescript --out-dir apps/web/pkg target/wasm32-unknown-unknown/release/grootscape_web.wasm
python3 -m http.server 8765 --directory apps/web
```

Open <http://localhost:8765>. The status line shows the engine version and whether the browser uses WebGPU or WebGL.

### macOS app

```sh
cargo build --locked --release --package grootscape-native
swift run --package-path apps/macos GrootscapeMac
```

Open `apps/macos/Package.swift` in Xcode to work on the app after building the engine.

### Android app

Open `apps/android` in Android Studio and run the `app` configuration, or build from the command line:

```sh
cd apps/android && ./gradlew assembleDebug
```

Gradle builds the engine for each ABI with `cargo-ndk` before packaging.

## Verify a change

```sh
cargo fmt --all --check
cargo clippy --workspace --all-targets --locked -- -D warnings
cargo test --workspace --locked

dotnet restore Grootscape.slnx --locked-mode
dotnet format Grootscape.slnx --verify-no-changes --no-restore
dotnet build Grootscape.slnx --configuration Release --no-restore
dotnet test --solution Grootscape.slnx --configuration Release --no-build
```

GitHub Actions also runs the Rust and .NET checks on Linux, macOS, and Windows, smoke-tests the desktop window and the Swift app on macOS, builds the browser client and the Android APK, and probes the server container. Commit the affected lockfile with any dependency change. See [AGENTS.md](AGENTS.md) for the contribution rules and [SECURITY.md](SECURITY.md) to report a vulnerability.
