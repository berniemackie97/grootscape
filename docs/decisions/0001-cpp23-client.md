# 0001: C++23 client foundation with SDL3

Status: selected for migration; merge requires owner review.

## Context

Main `d663f58d31d730c5c19b979e84b5725448745952` contains a clear-screen renderer, desktop CLI and lifecycle, a browser host, protocol v1 client codec, and version-only JNI/C hosts. It contains no client scene, input mapping or network session. The server remains .NET 10. The owner authorized the C++ migration and delegated dependency selection for the overall project standard.

## Decision

Use C++23 with CMake/Ninja and SDL3 3.4.8 at commit `d9d5536704d585616d4db3c8ba3c4ff6fc2757e1`. Verify the source archive SHA-256 in CMake. SDL uses the zlib license; its source distribution retains its license. No third-party product data enters this migration.

Use SDL's renderer for the current clear-screen contract. Native selection can use Metal, Direct3D, OpenGL or software according to platform availability. Emscripten uses OpenGL ES translated to WebGL 2, with minimum and maximum WebGL versions set to 2. Hardware selection remains SDL's responsibility; software rendering is available and tested explicitly. Report the renderer name at startup.

Dawn/WebGPU would preserve the former browser backend preference but introduces another graphics stack and toolchain for no current shader or scene requirement. SDL GPU is also premature for the existing clear-only behavior and does not itself provide the browser fallback. Neither is hidden behind a speculative renderer interface. Reassess the 3D API with #37's terrain requirements before adding scene rendering. This choice does not establish SDL's 2D renderer as the eventual 3D engine.

This is an explicit backend compatibility change: browsers use WebGL 2 even when WebGPU exists. Current clear-screen functionality and platform host behavior remain the acceptance boundary. Do not claim backend parity or future 3D performance from clear-screen smoke tests.

## Ownership and failure

Use values for wire messages and unique_ptr with SDL deleters for graphics resources. Hosts own lifecycle and windows. No owning raw pointers cross the API. The renderer's window pointer is borrowed and documented. JNI returns a JVM-owned string. C strings have static lifetime. Browser exports keep one session because the page owns one canvas; they catch C++ errors at the C/JavaScript boundary.

Warnings are errors for first-party code. Apply format, clang-tidy, ASan/UBSan and TSan checks separately. Toolchain and system library differences prevent a claim of bit-identical builds across machines. Dependency source identity and preset configuration are reproducible.

## Sources

- [SDL Emscripten integration](https://wiki.libsdl.org/SDL3/README-emscripten)
- [SDL renderer selection](https://wiki.libsdl.org/SDL3/SDL_HINT_RENDER_DRIVER)
- [SDL GPU scope](https://wiki.libsdl.org/SDL3/CategoryGPU)
