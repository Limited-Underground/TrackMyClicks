# TrackMyClicks

[Apache License 2.0](LICENSE) · [Contributing](CONTRIBUTING.md) · [Build instructions](docs/BUILD.md) · [Current status](docs/PROJECT_STATUS.md)

Copyright 2026 Nenad Bjelanovic. Limited Underground is the project brand and publisher.

The V0.4 assignment establishes the open-source product foundation. Application behavior remains the V0.3 baseline (0.3.0); no new public binary release is claimed.
Limited Underground · Every Click Counts.

Windows 11 x64 experimental product. No public binary release is established yet. For an existing owner-evaluation package, extract the entire ZIP into a folder, then double-click **TrackMyClicks.exe**. Keep the host/ and shared/ runtime folders alongside it. No installer or separate .NET installation is required. Close another TrackMyClicks version before launching; V0.2 is preserved separately.

V0.3 adds current/longest active-day streaks, six dated personal records, a real rolling 60-second click count, 51 tiered badges, three deterministic daily challenges, rotating offline activity facts, and a dedicated achievement showcase. **Show all** opens the showcase; **Back to dashboard** or Escape returns. Bronze, Silver, Gold, Platinum and Diamond thresholds live in Core/ExpansionRules.cs.

Challenges are optional goals for normal computer use. Assignments and targets stay fixed for the local day; each completed assignment grants 25 points exactly once. Earned receipts and points survive restart. Skipping a goal and taking breaks are always fine. Streak milestones are 3, 7, 14, 30, 60 and 100 active local-calendar days. A day requires recorded input; opening the app or staying paused does not make a day active. Yesterday's streak remains current until a real inactive-day gap.

The speedometer shows actual observed clicks in the last 60 seconds, without multiplying a partial startup interval. Paused input is excluded and the window continues aging. Restart starts an empty live window while preserving the all-time best and the day's best used by challenges. Personal-record notifications are limited to once per record category per local day. The notification checkbox controls achievement, challenge and record balloons; Windows may suppress their display.

Pause is available on the dashboard and tray menu. Minimize hides the dashboard in the notification area; double-click the tray icon or choose Open dashboard to return. Close (X) or tray Exit saves and stops tracking. Showcase windows close when the app is minimized or exits.

Data stays in %LOCALAPPDATA%\Limited Underground\TrackMyClicks\activity.sqlite3. Before upgrading an existing V0.2 database, V0.3 creates a uniquely named coherent SQLite backup beside it: activity.sqlite3.pre-v03-<UTC>-<unique>.bak. Migration is forward-only; newer/unsupported schemas or malformed expansion state are rejected rather than reset. Existing totals, settings and the original nine earned badges are retained. V0.2 did not store unlock dates, so imported badges explicitly show that the original date is unavailable.

Aggregate data saves every five seconds, on new rewards/record notices, on pause and on normal exit. Rewards, notice latches and qualifying totals commit atomically before notifications are emitted. Abrupt termination can lose up to five seconds of ordinary activity. Data and backups are local and unencrypted. The diagnostic --capture option exports only this app's own interface when explicitly requested; normal tracking does not record screenshots.

Never stores key identities, typed text, passwords, clipboard contents, app/window names or browsing history. Keyboard identities exist transiently only to suppress held-key repeats and are discarded on release. There are no accounts, telemetry, network calls, cloud services or leaderboards.

Distance is estimated pointer-coordinate travel, not physical mouse travel. Current pixels-per-inch calibration reinterprets historical raw pointer distance, matching V0.2. Movement personal records use raw pixels for that same interpretation. Completed badges/rewards remain earned; challenge progress retains its observed high-water value. Scroll activity combines absolute vertical and horizontal wheel deltas; 120 units is one notch. Facts use recorded totals, label estimates and refresh at most every 30 seconds.

Normal interactive desktop/session only; secure desktops, other sessions and some protected/elevated apps may not be observable. No special privileges are requested. This is an unsigned owner-evaluation prototype.

Source builds use .NET 10 SDK:
- dotnet build App/TrackMyClicks.csproj -c Debug
- dotnet run --project Tests/TrackMyClicks.Tests.csproj -c Release
- Focused migration/expansion/integration/buffer/WPF suites are under Tests.Migrations/, Tests.Expansion/, Tests.V03Integration/, Tests.V03Buffer/ and Tests.Visual/.
- Tests use disposable profiles; close other TrackMyClicks instances first. The WPF harness writes screenshots/results to evidence/v03/ui-tests/.
- Runtime packaging uses AppHostDotNetSearch=AppRelative and bundled Microsoft runtime folders with their license notices.

Publication status, validation limits and the pickup route are in docs/PROJECT_STATUS.md. Private validation evidence, owner-evaluation ZIPs, profiles and recovery records are excluded from this public source set.

Performance qualification: foreground-active WPF CPU on this host was about 66–73% of one logical core; the preserved V0.2 binary also measured about 73%. Background/ordinary hidden-launch samples were 2.3–3.7% of one core and roughly 148–159 MB working set. Root cause of the foreground rendering cost remains unproven; no cache, rendering-mode or OS workaround was shipped. This is a material known limitation for owner evaluation. Tests.Performance records active/background state and optional timer/hook isolation; it is not a claimed CPU-budget pass. Concurrent processes in different Windows sessions sharing one profile are unsupported.

License: original code is licensed under Apache License 2.0. See LICENSE, NOTICE and THIRD-PARTY-NOTICES.md. Preserve applicable third-party notices when distributing runtimes. See docs/RELEASING.md before publishing a binary.
