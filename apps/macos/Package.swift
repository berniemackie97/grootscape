// swift-tools-version: 6.0
import Foundation
import PackageDescription

let repo = URL(fileURLWithPath: #filePath)
    .deletingLastPathComponent()
    .deletingLastPathComponent()
    .deletingLastPathComponent()

// Built by `cmake --build --preset release`. Link the archive directly.
let engineArchive = repo.appending(path: "build/release/libgrootscape_native.a").path

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
