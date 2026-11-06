# TrackMyClicks architecture

Evidence basis: public 0.3.0 source at 60e0a7512033f3ce3af0d52e2545c46c409f8374 and separately preserved November 1 TMC-0002/4/5 overlay. Code in this documentation topic is the public baseline. The overlay is identified in the private preservation manifest, not silently published. [Reading route](FOUNDATION.md) · [Interfaces](INTERFACES.md) · [Recovery](RECOVERY.md).

## 1. Introduction and Goals

TrackMyClicks by Limited Underground is an experimental Windows activity counter: Every Click Counts. The owner needs daily/lifetime aggregate activity, local progress and a visible pause control without recording input content. Future maintainers need a reproducible distinction between public baseline, unpublished corrections and measured acceptance.

Intended quality goals: anonymous local-only tracking, passive desktop behavior and low resource cost. Implemented: click/press/pointer/wheel aggregates, SQLite, badges, streaks, records and daily challenges. The independently accepted same-host Core-only recovery adds106 executed assertions; neither it nor historical functional checks close foreground CPU or native usability acceptance. Social sharing is planned only in issue 3.

## 2. Architecture Constraints

Windows 11 x64, C#/.NET 10, WPF; System.Windows.Forms is used for the tray icon/menu, not a second application. Windows winsqlite3.dll supplies SQLite. No external NuGet package, database service, account, telemetry or application network client is required by identified source. Official Apache 2.0 LICENSE, NOTICE and third-party runtime obligations must be retained.

The November 2 freeze supersedes historical development permission. No SDK/framework migration, source refactor, owner-data use or runtime tracking is part of this foundation. Topic documentation and private preservation do not authorize publication of local overlays or a binary release.

## 3. System Scope and Context

The owner uses a local dashboard/tray. Windows supplies low-level keyboard/mouse messages and the native hook chain; TrackMyClicks observes transient state and must pass messages onward. The only persistent activity boundary is a local SQLite profile. No remote system participates at runtime. See the versioned [C4 model](architecture/workspace.dsl); official syntax validation and JSON/static export passed at its exact hash. Six exact inspection errors are retained without semantic/architecture acceptance; see the [model guide](architecture/README.md).

Build-time SDK downloads are separate from application behavior. Windows secure desktops, other sessions and some protected/elevated applications are outside reliable observation. Concurrent sessions sharing a profile are unsupported.

## 4. Solution Strategy

App hosts one WPF process; InputHooks emits anonymous observation events. MainWindow buffers observations in memory, then a 500 ms dispatcher tick drains them into ActivityEngine. Core derives progress and saves transactional aggregates. SQLite avoids a separate database service; bundled-runtime packages avoid requiring a separate runtime installation.

The public baseline automatically shows MainWindow. The unpublished P0 overlay explicitly constructs a tray-first tracker and defers hidden presentation. These are distinct implementations. Native nonintrusion and current visible CPU are unresolved; documentation does not choose an unreviewed replacement architecture.

## 5. Building Block View

| Source | Responsibility |
| --- | --- |
| App/App.xaml and App.xaml.cs | WPF lifetime; baseline StartupUri versus overlay explicit tray-first startup |
| App/InputHooks.cs | WH_MOUSE_LL / WH_KEYBOARD_LL, repeat suppression and native forwarding |
| App/MainWindow.xaml and MainWindow.xaml.cs | UI, tray, in-memory buffering, local-day boundaries, sample/save scheduling |
| App/ExpansionPresentation.cs and card/showcase views | Presentation of aggregate-derived progress |
| Core/ActivityEngine.cs | Daily/lifetime totals, pause/settings, original badges, atomic saves |
| Core/ActivityInsights.cs and InsightModels.cs | Streaks, rolling rates, personal records, stamped unlocks, daily reward receipts |
| Core/ExpansionRules.cs | Badge tiers and deterministic challenge definitions |
| Core/Store.cs | Native SQLite access, schema admission, coherent pre-migration backup and transactions |
| Tests*, including local-only Tests.Background | Focused synthetic tests; some App suites install real hooks |

Core is a library in the desktop process, not an independent C4 runtime container. SQLite is the datastore container, not a network server. Test/profiling programs are separate diagnostic executables, not shipped services.

## 6. Runtime View

Normal input: Windows callback -> transient repeat/pointer state -> anonymous click/key/move/scroll event -> in-memory buffer -> dispatcher drain -> totals/insights -> save -> committed notices. Key identities never reach storage. Mouse-coordinate differences are accumulated as raw pixel distance; coordinates are not persisted as a path.

Pause drains already observed activity and excludes subsequent activity until resume. Rolling 60-second samples age while paused; restart begins an empty live window while keeping historical bests. Local-day buffering preserves observation day across a delayed midnight tick. Original unlocks, expanded unlocks and daily challenge receipts commit with qualifying totals before notification publication.

Overlay-only lifetime: hidden tray startup -> explicit Open/double-click -> visible dashboard -> minimize hides -> Exit/Close saves/releases hooks, timer, tray, store and mutex. Observer failures are retained in memory and forwarded downstream; dispatcher stops failed tracking quietly. Baseline lacks these P0 changes. No native acceptance is inferred from this traced flow.

## 7. Deployment View

Windows 11 interactive desktop, one owner process and one local profile. The portable evaluation ZIP contains TrackMyClicks.exe plus matching host/ and shared/ runtimes and license notices. Development can invoke the built DLL using an installed .NET runtime; the app host is configured for app-relative runtime discovery. No installer, service or automatic-start requirement exists.

Source targets .NET 10 but does not pin an exact SDK with global.json; clean-build reproducibility must record the resolved SDK/desktop packs. Original package contents and runtime versions must come from their manifests, not be inferred from an installed SDK. Exact rebuild byte equality has not been demonstrated by this foundation.

## 8. Crosscutting Concepts

Privacy: no key identities/content, passwords, clipboard, screenshots during normal tracking, applications, titles or browsing history in persistence. Local aggregates are unencrypted and may still be private. The explicit --capture diagnostic exports this app's interface; never include real-profile captures in public docs.

Durability: SQLite WAL and synchronous FULL; periodic saves every tenth 500 ms dispatcher tick (nominally five seconds), plus reward/pause/normal-exit commits. Dispatcher delays can extend the save interval; no strict five-second maximum loss is established. Abrupt termination may lose activity since the last successful save. UTC labels backup filenames; local calendar dates own daily totals/challenges. Pointer calibration reinterprets raw historical distance. See schema/version rules in [Interfaces](INTERFACES.md).

Notifications: public baseline defaults on; unpublished P0 defaults off for new profiles and honors saved 1/0 preferences. Optional Windows balloons can be suppressed by Windows. The overlay-only hidden rendering suppression does not disable counting, rate expiry or persistence; that behavior is not integrated into baseline code.

## 9. Architectural Decisions

| Decision/evidence | Rationale and scope |
| --- | --- |
| Original prototype brief | Windows desktop, aggregate-only local privacy, no installer/cloud/service |
| [TMC-0001](https://github.com/Limited-Underground/TrackMyClicks/issues/1), completed | Public Apache 2.0 source, personal copyright; Limited Underground brand/publisher |
| [TMC-0002](https://github.com/Limited-Underground/TrackMyClicks/issues/2) | Measure CPU; hidden-only partial improvement does not establish foreground acceptance |
| [TMC-0004](https://github.com/Limited-Underground/TrackMyClicks/issues/4) | Fixed shell/tab navigation in unpublished overlay; final native pass pending |
| [TMC-0005](https://github.com/Limited-Underground/TrackMyClicks/issues/5) | Passive background operation; local correction, native cause/acceptance still open |
| November 2 foundation owner brief | Indefinite development freeze; preserve and document without resuming behavior work |

The coordinator reconciled the previously stale portfolio register on November 2 from the actual November 1 owner product decision: seventeen members, sixteen nonparked and Home Assistant Apps parked. This records the existing product rather than creating a new repository, Project, license, exposure or feature. No registration/trademark claim is invented. No new architectural decision is implemented merely to fill this document.

## 10. Quality Requirements

| Scenario | Source/test evidence | Remaining gate |
| --- | --- | --- |
| Input content never persists | InputHooks -> anonymous events; Store tables; source inspection | Independent privacy review; no owner-profile inspection needed |
| Pause excludes input, daily/lifetime totals survive restart | Accepted public Core/migration/expansion suites plus external restore:106 assertions; historical buffer results are separate | Native hook/dispatcher and private-overlay acceptance remain open |
| Unlock/reward once, migration preserves generated data | Accepted19 migration/46 expansion/26 restore checks; external seed has zero rewards, expansion tests earn25 points | Real data and authentic V0.2-binary integration not replayed; private fixture remains a dependency |
| Background counting does not open dashboard | Local P0 startup/forwarding suite, 43 historical checks | Native tray/focus/input acceptance remains frozen/unexecuted |
| Lightweight visible/hidden/paused operation | Retained CPU measurements | Valid matched visible/native comparison still unresolved |

## 11. Risks and Technical Debt

Foreground CPU historically reached roughly 72% of one logical core; measured UI Automation dispatcher bursts had an unidentified requesting client. Ordinary rendering alone is not a proven cause. Controlled P0 hidden samples were 1.17% idle, 2.73% callback activity and 1.04% paused, 116–121 MiB; short instrumented samples are not a resource-budget pass or a comparison to a different layout.

Exact reported screen hijacking remains unproven. Startup/diagnostic activation paths were identified; native testing stopped on physical Escape. Hooks share the WPF dispatcher thread, leaving input latency under dispatcher starvation unaccepted. Final tab geometry/DPI capture and real OS display-scaling acceptance remain open. Supported-Version JSON with omitted collections can be admitted as empty defaults; complete semantic validation is unresolved. Preserve/quarantine uncertain state before any restore executor opens it. This is a static source finding, not an executed failure or authorization to change behavior. Accepted same-host public Core/generated-data recovery does not establish a hermetic/new-machine rebuild, desktop/native or owner-data acceptance, accepted-binary equality or a public release. Six model-inspection findings remain visible.

## 12. Glossary

TrackMyClicks: product name. Limited Underground: brand/publisher. Profile: local activity SQLite state, not an account. Lifetime: aggregate row with day key lifetime. Notch: absolute wheel delta divided by 120. Pointer distance: coordinate travel estimated via pixels-per-inch, not physical mouse travel. Active day: local calendar day with input. Overlay: preserved uncommitted source distinct from public main. Demonstrated: executed evidence at named inputs/scope. Foundation: preservation/documentation/recovery work, not resumed development.
