# Build and validate

Use Windows 11 x64 with the .NET 10 SDK, including its Windows Desktop targeting pack. No proprietary SDK, external NuGet package or database server is required. Build restore can contact the SDK's configured package sources; the running application has no network functionality.

From the repository root in PowerShell:

```powershell
dotnet build App/TrackMyClicks.csproj -c Debug
dotnet build App/TrackMyClicks.csproj -c Release
dotnet run --project Tests/TrackMyClicks.Tests.csproj -c Release
dotnet run --project Tests.Migrations/TrackMyClicks.Migrations.Tests.csproj -c Release
dotnet run --project Tests.Expansion/TrackMyClicks.ExpansionTests.csproj -c Release
```

Run desktop suites only in an unlocked interactive test session, after closing other TrackMyClicks instances:

```powershell
dotnet run --project Tests.V03Buffer/TrackMyClicks.V03Buffer.csproj -c Release
dotnet run --project Tests.Visual/TrackMyClicks.VisualTests.csproj -c Release
dotnet run --project Tests.Performance/TrackMyClicks.PerformanceTests.csproj -c Release
```

These suites create disposable profiles/output. Inspect their arguments and default output paths before use. They may briefly install input hooks and show test windows. They do not represent human acceptance or a CPU-budget pass.

Tests.V03Integration additionally requires the preserved V0.2 Core binary for its authentic upgrade fixture. It is intentionally not distributed with this source. Its first argument is a fixture root containing output/TrackMyClicks-v0.2-win-x64/TrackMyClicks.Core.dll; without it the authentic-binary upgrade test cannot run. The core regression, migration and expansion suites do not require this private artifact.

The App project expects an app-local runtime when launched by its executable. For development with the installed runtime, launch the built assembly:

```powershell
$env:TRACKMYCLICKS_DATA_DIR = Join-Path $PWD 'artifacts/dev-profile'
dotnet App/bin/Debug/net10.0-windows/TrackMyClicks.dll
Remove-Item Env:TRACKMYCLICKS_DATA_DIR
```

TRACKMYCLICKS_DATA_DIR overrides the normal local profile directory. Always set a disposable directory while testing. Do not launch another version against the owner's profile. The optional --capture=PATH switch saves only the application's own UI on explicit request; normal tracking does not capture screenshots.

For a portable x64 candidate with bundled runtime:

```powershell
dotnet publish App/TrackMyClicks.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

This requires matching runtime packs at restore. If unavailable, use framework-dependent publish with a complete matching Microsoft runtime distribution in host/ and shared/, retaining its LICENSE.txt and ThirdPartyNotices.txt; verify app-local discovery on a clean extraction. Do not silently copy arbitrary SDK/runtime versions. Include project LICENSE, NOTICE and THIRD-PARTY-NOTICES.md in every distribution. Follow RELEASING.md before sharing a binary.
