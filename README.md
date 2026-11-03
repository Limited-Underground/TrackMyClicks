# TrackMyClicks

**Every Click Counts.** Limited Underground is the project brand and publisher. Copyright 2026 Nenad Bjelanovic. [Apache License 2.0](LICENSE) · [Notices](NOTICE) · [Third-party obligations](THIRD-PARTY-NOTICES.md).

**Current state — November 3, 2026:** product development remains frozen indefinitely. This unmerged documentation topic is based on source `60e0a7512033f3ce3af0d52e2545c46c409f8374` (functional baseline 0.3.0). Current authorization covers documentation reconciliation only. Reading these recipes does not authorize a build, tracking launch, profile inspection, release or merge.

## Start here

| Need | Maintained route |
| --- | --- |
| Current owner, source, accepted evidence and next action | [Current status](docs/PROJECT_STATUS.md), [foundation reading path](docs/FOUNDATION.md) |
| Purpose, requirements, architecture and remaining quality gates | [Architecture](docs/ARCHITECTURE.md), [quality requirements](docs/ARCHITECTURE.md#10-quality-requirements) |
| Windows input, data/schema and packaging contracts | [Interfaces](docs/INTERFACES.md) |
| Build prerequisites and Core versus desktop tests | [Build and testing](docs/BUILD.md) |
| Declared source and generated-data recovery | [Recovery](docs/RECOVERY.md) |
| Privacy boundaries and vulnerability reporting | [Security](SECURITY.md) |
| Contributor/agent rules and future release procedure | [Contributing](CONTRIBUTING.md), [AGENTS](AGENTS.md), [Release](docs/RELEASING.md) |

The [dedicated Project 17](https://github.com/orgs/Limited-Underground/projects/17) owns live task status. [Issue6](https://github.com/Limited-Underground/TrackMyClicks/issues/6) tracks this frozen foundation. Local documentation records evidence; it does not replace the Project or grant execution.

## Product and source boundaries

The Windows 11 x64 experimental baseline counts left/right/middle clicks, keyboard presses, pointer-coordinate distance and wheel activity. It provides daily/lifetime totals, visible pause, a tray menu, SQLite persistence, legacy badge compatibility and 51 tiered achievements, active-day streaks, six personal records, a rolling60-second click count and three deterministic daily challenges. A completed daily challenge grants25 points once; pauses do not create activity. Optional notices use Windows balloons. Distance is estimated using pixels per inch; calibration reinterprets historical raw distance. Wheel activity combines absolute vertical/horizontal deltas, 120 units per notch.

The baseline opens its dashboard at startup and enables notices for new profiles. Performance, compact-tab navigation and tray-first/default-off-notice corrections from issues2/4/5 remain in a separately preserved private local overlay. They are absent from this candidate's code and unaccepted for integration. Native focus/input/tray behavior, final geometry/DPI and visible CPU acceptance remain open. Social sharing is planned only in issue 3.

## Evidence and remaining gates

The accepted November 2 same-host Core recovery used fresh HTTPS source and declared installed SDK 10.0.303/targeting pack 10.0.11. Four Release build invocations and 106 actual assertions passed: 15 aggregate, 19 migration, 46 expansion, 26 external generated-data restore. The host inventory was 10.0.14; the executed CLR version was not separately traced. This is not a complete SDK archive, hermetic/new-machine reconstruction, desktop build, owner-data restore or binary-equality result. See [Recovery](docs/RECOVERY.md) for exact scope and private executor availability.

Official Structurizr syntax/JSON/static export passed for the unchanged model. Its [six inspection errors](docs/architecture/README.md#retained-inspection-findings) remain visible; neither handbook presentation nor documentation review establishes architecture acceptance or ISO conformance.

Foreground WPF CPU historically reached about 66–73% of one logical core. Low hidden-state samples do not establish visible low-resource operation or prove the screen-hijacking cause. These and the other [V1 readiness gaps](docs/PROJECT_STATUS.md#remaining-v1-readiness-gaps) remain disclosed, not resolved by this documentation.

## Privacy and evaluation artifacts

No key identities/content, passwords, clipboard contents, application/window names, browsing history or screenshots during normal tracking are persisted. Key identities exist transiently for repeat suppression. There are no accounts, telemetry, cloud services or application network calls. Aggregates and backups are local, unencrypted and potentially private. The normal profile is under `%LOCALAPPDATA%\Limited Underground\TrackMyClicks\activity.sqlite3`; it is never a foundation fixture. Save ticks are nominally five seconds, with no strict maximum loss bound. See [Interfaces](docs/INTERFACES.md) for migration/completeness limitations.

Earlier V0.1/V0.2/V0.3 unsigned evaluation ZIPs and their evidence remain preserved privately; no supported public binary release is established. They require the complete application/runtime folders and notices. Historical launch/packaging recipes are in [Build](docs/BUILD.md) and [Release](docs/RELEASING.md), with execution boundaries intact. Secure desktops, other sessions and some protected/elevated applications may not be observable; concurrent sessions sharing one profile are unsupported.
