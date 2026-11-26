import GrootscapeEngine
import SwiftUI

@main
enum Launcher {
    @MainActor
    static func main() {
        if CommandLine.arguments.dropFirst().elementsEqual(["--version"]) {
            print(GrootscapeApp.engineVersion)
        } else {
            GrootscapeApp.main()
        }
    }
}

struct GrootscapeApp: App {
    static let engineVersion = String(cString: grootscape_engine_version())

    var body: some Scene {
        Window("Grootscape", id: "startup") {
            Text("Grootscape \(Self.engineVersion)")
                .frame(minWidth: 480, minHeight: 300)
        }
    }
}
