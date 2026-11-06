# Build and testing

[Status](PROJECT_STATUS.md) · [Requirements](ARCHITECTURE.md#10-quality-requirements) · [Interfaces](INTERFACES.md) · [Recovery](RECOVERY.md) · [Security](../SECURITY.md) · [Release](RELEASING.md).

**Build and test recipes require separate authorization.** Product development remains frozen. The November 3 reconciliation used offline documentation checks. The accepted November 2 Core result below applies to public baseline `60e0a751`. Tests.Background and the private performance, tab and tray changes are retained outside that baseline. [Current status](PROJECT_STATUS.md)

## Declared prerequisites

Use Windows 11 x64 and a declared .NET10 SDK. Core/test projects require the .NET reference pack and Windows-supplied winsqlite3.dll; App/WPF projects additionally require the Windows Desktop targeting/runtime packs. No third-party NuGet package or database server is declared. Restore may contact configured sources unless explicitly cleared; application runtime network behavior is separate.

The accepted same-host Core run pinned SDK 10.0.303 with rollForward disabled, installed Microsoft.NETCore.App.Ref 10.0.11, fresh CLI/NuGet/TEMP directories and cleared feeds/fallbacks. Host inventory was 10.0.14; actual executed CLR was not separately traced. Source repository has no global.json or lock file. Do not infer a complete SDK archive, new-machine proof or historical package equality from these installed-host inputs.

## Core-only tests and accepted result

These public executables create generated disposable files beneath their own output directories and do not reference the App project. A future separately granted run can use:

~~~powershell
dotnet run --project Tests/TrackMyClicks.Tests.csproj -c Release
dotnet run --project Tests.Migrations/TrackMyClicks.Migrations.Tests.csproj -c Release
dotnet run --project Tests.Expansion/TrackMyClicks.ExpansionTests.csproj -c Release
~~~

November 2 accepted evidence:15 aggregate, 19 migration and 46 expansion assertions, plus 26 checks in an external Core-only restore executor; four Release build invocations, zero compiler warnings/errors. The external executor is not in this public source set. [Recovery](RECOVERY.md) describes its identity, supply gap, schema 0 backup-to-second-fixture and closed schema 1 JSON restoration. Only that generated-data scope is demonstrated.

The first diagnostic `dotnet --info` failed while querying the Windows Service Control Manager. The retained continuation used `dotnet --version`, `MSBuildEnableWorkloadResolver=false` and `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true` for Core-only builds; it did not elevate, modify services or install/update workloads. The first build's nonfatal PATH/workload-verification messages remain in the evidence.

## Desktop and hook suites — separate authorization

The following historical recipes can build or run WPF/native-hook code. They were not executed by the Core-only recovery or this documentation task:

~~~powershell
dotnet build App/TrackMyClicks.csproj -c Debug
dotnet build App/TrackMyClicks.csproj -c Release
dotnet run --project Tests.V03Buffer/TrackMyClicks.V03Buffer.csproj -c Release
dotnet run --project Tests.Visual/TrackMyClicks.VisualTests.csproj -c Release
dotnet run --project Tests.Performance/TrackMyClicks.PerformanceTests.csproj -c Release
~~~

A future authorized desktop run needs an unlocked interactive test session, an explicit disposable profile, declared screenshot/output locations and no conflicting tracker instance. These suites may install real hooks and show windows even if a harness offers hidden operation. They do not establish human/native acceptance or a CPU-budget pass.

Tests.V03Integration needs the actual preserved V0.2 Core DLL as an additional private fixture; it is not distributed here. Its first argument is a fixture root containing `output/TrackMyClicks-v0.2-win-x64/TrackMyClicks.Core.dll`. This dependency is unnecessary for the accepted public Core/migration/expansion suites. Never substitute the owner's activity data.

## Historical launch and packaging recipes

The apphost expects an app-relative runtime. When separately authorized to use the installed runtime, the development route is the built DLL with a new disposable profile:

~~~powershell
$env:TRACKMYCLICKS_DATA_DIR = Join-Path $PWD 'artifacts/dev-profile'
dotnet App/bin/Debug/net10.0-windows/TrackMyClicks.dll
Remove-Item Env:TRACKMYCLICKS_DATA_DIR
~~~

The explicit `--capture=PATH` diagnostic exports this app's UI; normal tracking does not take screenshots. It is outside current documentation authority. Never launch a test against the normal local profile.

A future portable x64 candidate can use `dotnet publish App/TrackMyClicks.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64`. Matching runtime packs must be available. Historical evaluation ZIPs instead used framework-dependent publishing plus complete matching host/shared runtime distributions after pack access failed. Preserve exact runtime LICENSE.txt/ThirdPartyNotices.txt and project LICENSE/NOTICE/THIRD-PARTY-NOTICES.md; do not copy arbitrary SDK versions. Verify a fresh extraction and follow [Release](RELEASING.md). No publication or installer is authorized here.
