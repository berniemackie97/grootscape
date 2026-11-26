// swift-tools-version: 6.0
import Foundation
import PackageDescription

let repo = URL(fileURLWithPath: #filePath)
    .deletingLastPathComponent()
    .deletingLastPathComponent()
    .deletingLastPathComponent()

let package = Package(
    name: "GrootscapeMac",
    platforms: [.macOS(.v14)],
    products: [.executable(name: "GrootscapeMac", targets: ["GrootscapeMac"])],
    targets: [
        .systemLibrary(name: "GrootscapeEngine"),
        .executableTarget(
            name: "GrootscapeMac",
            dependencies: ["GrootscapeEngine"],
            linkerSettings: [
                .unsafeFlags(["-L", repo.appending(path: "target/release").path]),
            ]
        ),
    ]
)
