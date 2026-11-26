# C++23 migration evidence

## Audited baseline

Repository: berniemackie97/grootscape. Main: `d663f58d31d730c5c19b979e84b5725448745952`.

Audited all tracked client sources, platform hosts, protocol fixtures, server test entry points, repository rules and CI. Remote branches: main; merged setup/rust-client-skeleton; three independent Dependabot updates (.NET packages and Docker SDK 11). No unmerged client feature branch exists. Do not upgrade the server to SDK 11 as part of this migration.

Audited all issue titles and relevant acceptance criteria. Migration #166 belongs to foundation epic #11 through a native parent relationship. Rendering #14, networking #13 and hosts #16 remain separate. Issues #36, #37, #43, #44 and #45 describe future capabilities, not existing behavior. #161's hidden-canvas startup defect receives a focused browser regression. No gameplay issue is closed by this migration.

## Parity gates

| Baseline | C++ replacement | Evidence |
|---|---|---|
| v1 hello encoding, welcome/reject decoding | dependency-free protocol target | canonical JSON fixtures compiled into tests; malformed data and error precedence |
| Desktop clear, resize, close and strict CLI | SDL host and shared renderer | CLI checks, pixel readback, resize, repeated lifetime test, three-frame smoke |
| Browser clear and canvas sizing | Emscripten exports and page host | real Chromium WebGL 2 checks, hidden startup, resized canvas and clear pixels |
| Static C version symbol for Swift | static C++ library, unchanged symbol | Swift package build and matching version |
| Android JNI version and UI | Gradle externalNativeBuild, unchanged symbol | both ABI libraries, instrumented version/UI tests |
| .NET protocol/server | unchanged implementation | 39 server tests and live C++/.NET WebSocket handshake |

Rust baseline tests passed in a detached worktree at the audited main commit. All 12 C++ migration parity jobs passed at `935def3` in [run 45894839744](https://github.com/berniemackie97/grootscape/actions/runs/45894839744) before Rust sources, Cargo manifests/lockfile, Rust toolchain settings and the Cargo-NDK Gradle task were removed. This includes desktop tests, window smoke and C++/.NET integration on all three desktop platforms; Swift linkage; Android packaged ABI checks and emulator instrumentation; Chromium browser checks; clang-tidy/format; ASan/UBSan; TSan; .NET tests on three systems; and the server container.

Local validation passed on Apple Silicon macOS: 3 native CTest entries, 3 browser tests (including context loss and reload), the Metal three-frame smoke, Swift version linkage, both Android debug/test APK ABI builds, clang-tidy 22.1.8, ASan/UBSan, TSan, all 39 .NET tests, and live C++/.NET protocol integration. Native release artifacts also built and tested with an explicit macOS 14 deployment target. CI uses a reduced mmap randomization setting for the Linux TSan runtime; this changes the test runner, not product behavior.

The final dependency review pins SDL 3.4.20, the current stable patch release at review time; final checks cover this pin. PR #167 records the final head and its CI result after removal. Main remains unchanged pending owner approval. Roadmap #166 stays In Review; no acceptance or gameplay issue is closed before merge.

## Limits

Software pixel tests and hosted-runner window tests do not certify physical GPU drivers. Android emulator checks do not certify physical ARM devices. Browser tests cover Chromium WebGL 2, not all browser/device combinations. WebGPU selection changes deliberately; see ADR 0001. Android and Swift rendering were not present in baseline and are not added here. No gameplay, performance or production-readiness claim follows from this foundation migration.
