# Grootscape

Grootscape is a self-hosted multiplayer RPG project for a small private playtest. The MVP targets the browser, macOS, Windows, Linux, and Android, with iOS in a later milestone. The first-party native clients use a Grootscape game engine. The macOS app uses Swift and Xcode, and the Android app uses Kotlin and Android Studio.

The upstream [TSPS project](https://github.com/RSPSApp/tsps) provides the starting point for the TypeScript browser client. Browser interoperability targets a pinned TSPS revision. Grootscape's release cadence is independent of official client updates.

The foundation runs an ASP.NET Core host with `GET /health/live` and verifies that endpoint through the application pipeline. The probe checks process liveness.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The minimum SDK for this checkout is `10.0.403`. Use `10.0.404` or a newer patch in that feature band.

```sh
dotnet restore Grootscape.slnx --locked-mode
dotnet run --project src/Grootscape.Server --no-restore -- --urls http://127.0.0.1:5080
```

`curl --fail http://127.0.0.1:5080/health/live` returns `Healthy` while the host is responding.

## Verify a change

```sh
dotnet restore Grootscape.slnx --locked-mode
dotnet format Grootscape.slnx --verify-no-changes --no-restore
dotnet build Grootscape.slnx --configuration Release --no-restore
dotnet test --solution Grootscape.slnx --configuration Release --no-build
```

GitHub Actions runs the same checks on Linux, macOS, and Windows. Commit each affected `packages.lock.json` when changing a dependency.
