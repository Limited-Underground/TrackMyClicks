# Input, storage and packaging interfaces

[Requirements/architecture](ARCHITECTURE.md) · [Testing](BUILD.md) · [Security](../SECURITY.md) · [Recovery](RECOVERY.md) · [Status](PROJECT_STATUS.md). Source: public 60e0a75 plus explicitly labeled private TMC-0005 changes; this topic contains public baseline code.

## Windows observation boundary

App/InputHooks.cs installs Windows standard low-level mouse/keyboard hooks (14/13). Left/right/middle button-down count independently. Key-down counts once until matching key-up; virtual keys live transiently in a set for repeat suppression only. Pointer deltas produce Euclidean pixel distance; absolute vertical/horizontal wheel deltas combine, with 120 units per notch.

The contract is observation and downstream forwarding, never intentional suppression. App observers consume click kind, press count, distance and wheel delta rather than key identities. Callbacks must be fast, memory-only, with no database/UI/network work. The P0 overlay catches observer exceptions then returns the downstream result; baseline exception behavior differs. This is a custom anonymous aggregate boundary over standard Win32 APIs, not a new device protocol.

## SQLite contract

Core/Store.cs owns Windows winsqlite3.dll P/Invoke. No server connection, shared network store or ORM exists.

| Table | Meaning |
| --- | --- |
| totals(day PRIMARY KEY,l,r,m,k,d,s) | Local yyyy-MM-dd daily row and lifetime sentinel; raw distance pixels, wheel notches |
| achievements(id PRIMARY KEY) | Original earned badge IDs |
| settings(name PRIMARY KEY,value) | paused, notifications, metric as 1/0; ppi numeric calibration |
| expansion_state(id=1,json) | Version 1 aggregate-derived insight state |

PRAGMA user_version 0 admits legacy V0.2 or an empty profile; latest supported schema is 1. Existing legacy tables trigger a uniquely named coherent SQLite backup before migration. Schema creation and version advance are one transaction. New/future schema versions above 1 are rejected without downgrade. Invalid JSON syntax, unsupported/missing Version and explicit null required collections are rejected. Omitted collections with model initializers can instead be accepted as empty; for example, a Version-only object does not prove the historical collections were present. Successful admission is not completeness or data-recovery proof. This is a source limitation, not permission to rewrite, repair or reset any profile. Expansion JSON Version 1 includes active days, records, per-day rate peaks, record-notice latches, unlock stamps, deterministic challenge assignments/progress and completed reward receipts. It does not contain input content.

WAL and synchronous FULL apply after admission. Saves atomically replace today/lifetime totals, preserve original achievement IDs, persist settings and optionally expansion state. Core publishes committed reward/record notifications only after save succeeds. See Tests.Migrations for rollback, coherent online backup, future-version rejection and restart assertions. Do not change SQL/JSON or reuse a real profile to validate this documentation.

## Profile and package boundaries

TRACKMYCLICKS_DATA_DIR selects an explicit disposable profile for tests; otherwise the app uses the owner's local application-data directory. That default directory must never be a foundation fixture. A local named mutex prevents ordinary duplicate instances, but multiple Windows sessions sharing one profile are unsupported.

The pre-v03 backup is a restore input for a separate synthetic copy, never an instruction to overwrite a current profile. Do not copy a live database without its WAL consistency contract; use a coherent backup or a confirmed closed synthetic store. Reverse migration is unsupported.

Portable packages require the exact application/runtime contents, LICENSE, NOTICE and applicable Microsoft/third-party notices. Source has no third-party NuGet dependency; OS/runtime dependencies retain their own licenses. Build metadata and Apache licensing do not imply a signed installer, auto-start, public binary release, network service or external sharing integration.

## Contract evidence and remaining scope

The independently accepted November 2 Core run exercised generated totals/settings/earned IDs, rollback/admission, pause/rollover, once-only rewards and synthetic backup restoration at public 60e0a751. See [Build](BUILD.md) for15/19/46/26 assertion groups and [Recovery](RECOVERY.md) for seed/closure limits. It does not demonstrate native input delivery, dispatcher latency, owner-data completeness or a private overlay. The JSON completeness limitation above remains open; this documentation task changes no API, schema or runtime behavior.
