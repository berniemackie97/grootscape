// swift-tools-version: 6.0
import Foundation
import PackageDescription

let repo = URL(fileURLWithPath: #filePath)
    .deletingLastPathComponent()
    .deletingLastPathComponent()
    .deletingLastPathComponent()

// Built by `cargo build --locked --release --package grootscape-native`. The archive is linked by
// path so the linker does not pick the shared library that the same build produces for Android.
let engineArchive = repo.appending(path: "target/release/libgrootscape_native.a").path

let package = Package(
    name: "GrootscapeMac",
    platforms: [.macOS(.v14)],
    products: [.executable(name: "GrootscapeMac", targets: ["GrootscapeMac"])],
    targets: [
        .systemLibrary(name: "GrootscapeNative"),
        .executableTarget(
            name: "GrootscapeMac",
            dependencies: ["GrootscapeNative"],
            linkerSettings: [.unsafeFlags([engineArchive])]
        ),
    ]
)
