# Grootscape

Grootscape is a self-hosted multiplayer RPG. The authoritative server uses C# and .NET 10. The client foundation uses C++23. Desktop and browser hosts render a clear screen; Android and Swift macOS hosts report the shared engine version. Gameplay and mobile scene rendering remain planned work in [GitHub Issues](https://github.com/berniemackie97/grootscape/issues).

## Build and run

Install CMake 3.28 or later, Ninja, Python 3, and a C++23 compiler (Apple Clang 21, Clang 18, or Visual Studio 2022 or later). Use an MSVC developer shell on Windows. CMake downloads SDL3 by commit and verifies its SHA-256. First configuration needs network access. CLion can open the root directory and use the checked-in presets.

```sh
cmake --preset dev
cmake --build --preset dev
ctest --preset dev
build/dev/grootscape-client --exit-after-frames 3
```

Windows executables have an `.exe` suffix. Omit the frame limit for normal use. `--version` and `--help` do not initialize graphics. Linux needs development packages for X11 and OpenGL; CI lists the packages. Set `SDL_RENDER_DRIVER=software` for software rendering. The host reports its selected backend.

Install .NET SDK 10.0.403 or later (CI uses 10.0.404). Start the server with:

```sh
dotnet run --project src/server/Grootscape.Server
```

## Verify a change

```sh
cmake --preset dev
cmake --build --preset dev
ctest --preset dev
find src/client tests/client -type f \( -name '*.cpp' -o -name '*.hpp' -o -name '*.h' \) -print0 | xargs -0 clang-format --dry-run --Werror
cmake --preset tidy
cmake --build --preset tidy
cmake --preset asan
cmake --build --preset asan
ctest --preset asan
cmake --preset tsan
cmake --build --preset tsan
ctest --preset tsan

dotnet restore Grootscape.slnx --locked-mode
dotnet format Grootscape.slnx --verify-no-changes --no-restore
dotnet build Grootscape.slnx --configuration Release --no-restore
dotnet test --solution Grootscape.slnx --configuration Release --no-build
python3 tools/test-server-integration.py build/dev/protocol-peer
```

Use clang-format 22.1.8 and clang-tidy 18 or later. Put clang-tidy on PATH before configuring `tidy`. Sanitizer presets require Clang or GCC; use separate build directories because TSan cannot run with ASan. Tests include canonical protocol fixtures, invalid command inputs, C ABI version, renderer pixel readback, resize and repeated destruction, plus a C++/.NET WebSocket handshake through `/play`. Linux display tests need Xvfb or a desktop session. CI exercises all three desktop systems, the server container, Android and the browser.

### Swift macOS host

```sh
cmake --preset release
cmake --build --preset release
swift run --package-path apps/macos GrootscapeMac --version
swift run --package-path apps/macos GrootscapeMac
```

### Android host

Install JDK 17 and Android SDK 36. Gradle pins NDK 29.0.14206865 and CMake 3.31.6 and builds both arm64-v8a and x86_64 libraries. Set ANDROID_HOME or use an untracked `apps/android/local.properties` with `sdk.dir`.

```sh
cd apps/android
./gradlew assembleDebug assembleDebugAndroidTest --console=plain
./gradlew connectedDebugAndroidTest --console=plain
```

The second command needs a device or emulator. These hosts retain the current version display; rendering into Android SurfaceView remains #43.

### Browser host

Use [Emscripten SDK](https://github.com/emscripten-core/emsdk) commit `e4fe26ef59168ff44f4c23c466e497bf60b3411e`, install and activate `4.0.20`, then source `emsdk_env.sh`.

```sh
emcmake cmake -S . -B build/web -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=OFF
cmake --build build/web
cd tests/web
npm ci
npx playwright install chromium
npm test
```

Serve `apps/web` with a static HTTP server. Generated files go into `apps/web/pkg`. WebGL 2 is required. The page waits for a visible, nonzero canvas and pauses rendering when hidden. Browser tests check hidden startup, resize and clear pixels. No WebGPU-only path remains. See the [backend decision](docs/decisions/0001-cpp23-client.md) and [migration evidence](docs/verification/cpp23-migration.md).

See [architecture](docs/architecture.md), [contribution rules](AGENTS.md), [offline research](docs/research.md), and [security reporting](SECURITY.md).
