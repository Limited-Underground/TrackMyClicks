# Source, package and generated-data recovery

[Status](PROJECT_STATUS.md) · [Build/testing](BUILD.md) · [Interfaces](INTERFACES.md) · [Security](../SECURITY.md) · [Release](RELEASING.md).

The November 2 same-host Core rehearsal was independently accepted at the scope below. Use the current handoff to check authority for any future recovery run, build, desktop or hook execution, profile access or runtime migration. [Current status](PROJECT_STATUS.md)

## Declared source and custody

The accepted run obtained fresh HTTPS source at `60e0a7512033f3ce3af0d52e2545c46c409f8374` for functional baseline 0.3.0. All 49 ordinary inputs matched after compilation. Recovery and integration of the private performance, navigation and tray overlay remain separate. [Public source](https://github.com/Limited-Underground/TrackMyClicks/tree/60e0a7512033f3ce3af0d52e2545c46c409f8374)

The private November 2 checkpoint preserves51 ordinary canonical source files, including15 tracked modifications and two new Tests.Background files, raw Git/all refs, original evaluation ZIPs and selected receipts. Exact custody manifests remain with the existing task owner, excluded from public docs/handbook input. Profiles, build caches and other ignored historical trees were excluded and retained at origin. Independent internal-disk custody is not off-laptop protection. No custody recopy/audit was performed by this documentation maintenance.

| Original private package | SHA-256 |
| --- | --- |
| TrackMyClicks-v0.1-win-x64.zip | E5A5A5F0F3FA896D29F20D0BD047A5D3721321C151F7C025B022F87D1E3D097B |
| TrackMyClicks-v0.2-win-x64.zip | C1332EBA13B1CD6CF8290A90DAC5CEFB64941B43472969E86882D04BA60A51A8 |
| TrackMyClicks-v0.3-win-x64.zip | C4898AC833AB5E0A1C3EE061125959983902D0A7440F5B8A639DE354FF2DFE8C |

V0.3 is a retained evaluation artifact, not newly owner-accepted behavior. ZIP hashes match the checkpoint; do not imply public remote artifact availability. Original LICENSE/NOTICE/runtime notices stay with packages. Never upload private profiles, evidence or these packages as an incidental documentation action.


## Five distinct recovery claims — current result

| Claim | Demonstrated scope and limit |
| --- | --- |
| 1. Source/history retrievable | Declared source/Git custody remains verified at its recorded scope. Accepted fresh HTTPS materialization of public 60e0a751 provided the unchanged49-file build input. The local overlay is separate and not remotely recovered. |
| 2. Exact accepted binary retrievable/hash-matched | Original evaluation ZIPs hash-matched the retained checkpoint. This is artifact custody, not new owner acceptance, a supported public release or a remote-download claim. |
| 3. Source rebuild succeeds | Four same-host Release Core-only build invocations passed with declared installed tools. Full App/WPF reconstruction, complete SDK archive and hermetic/new-machine rebuild remain unproved. |
| 4. Rebuild byte equality | Not attempted or demonstrated; exact historical package/toolchain/runtime closure remains a separate gap. |
| 5. Runtime/data restoration | Generated schema 0 coherent-backup-to-second-fixture and closed schema 1 JSON copy/reopen passed. No desktop/hooks or actual owner profile was inspected, backed up or restored. |

## Accepted November 2 Core-only execution

Fresh HTTPS source at `60e0a7512033f3ce3af0d52e2545c46c409f8374`, with all 49 cloned source files unchanged after compilation. Exact SDK 10.0.303 was pinned with `rollForward: disable`; its installed Microsoft.NETCore.App.Ref 10.0.11 pack and host inventory 10.0.14 were declared; the actual executed CLR version was not separately traced. Fresh task-local CLI/NuGet caches, cleared package sources/fallback folders, one MSBuild node and disabled shared compilation were used. Assets contain only Core project references, with no restored NuGet packages. No historical checkout, build output, private DLL or activity profile satisfied the build.

Four Release builds exited zero with zero compiler warnings/errors. Actual executables passed 15 aggregate, 19 migration, 46 expansion and 26 external restore checks (106 total). The generated schema-0/WAL fixture produced a coherent backup, copied by exact hash into a second empty fixture and migrated without loss of seeded daily/lifetime totals, settings or legacy achievement IDs. A closed schema-1 copy retained exact expansion JSON and reopened without repeated migration. The external seed has zero completed challenge rewards; nonzero 25-point reward persistence and duplicate prevention were exercised separately by the expansion executable. This is generated-data recovery, not owner-profile or complete desktop recovery.

External executor Program.cs SHA-256: `567c767956048b401e2919b05890d398f66fa99ab438798bb42190d622bb0d93`. Its private task receipt is `core-recovery-run-01/CORE_RECOVERY_RESULT.json`, with explicit source/tool/dependency/operation/output manifests and `synthetic-restore/result.json`. The executor is compiled/tested but remains task-owned and is not remotely retrievable from the product repository. To reproduce, supply that exact executor source, its project and a fresh pinned Core source; use `dotnet build <executor.csproj> -c Release --configfile <cleared-config> -m:1 -nr:false -p:UseSharedCompilation=false -p:TrackMyClicksCoreProject=<fresh-Core.csproj>`, then `dotnet <executor.dll> <new-output-directory>` with `TRACKMYCLICKS_FOUNDATION_TASK_ROOT` explicitly set to the new task root. Never supply an existing/owner profile.

First diagnostic failure is preserved: `dotnet --info` could not query the Windows Service Control Manager under the restricted session. SDK selection succeeded with `dotnet --version`; Core builds succeeded with optional workload resolution/update notification disabled, without elevation, service changes or SDK installation. The first build emitted nonfatal CLI PATH/workload-verification messages; compiler results were zero warnings/errors.

Sampled child-process-tree working set peaked at 285,941,760 bytes (about 273 MiB); scratch at completion was 3,638,270 bytes. Observed C free space stayed above 44.69 GiB and available RAM above 22.77 GiB. Sampling does not prove an absolute transient peak. No additional heavy phase, App build, hooks, tracker launch, owner-data read, overlay integration or binary-equality claim. The frozen outputs received qualified independent same-host host-recovery acceptance on November 2 (America/New_York); full foundation completion, merge/release and product/native acceptance remain separate.

## Independent acceptance and reuse

The retained `host-recovery-acceptance-001` receipt independently checked the frozen recipe/log/output identities:279 declared files,3,838,846 bytes; run manifest SHA-256 `a43d6598b84395bed1f37bcca10d155bfe0742f116f7a46dbda4c8aaebf8d6bc`, Core result SHA-256 `58b888ba778ac5234e48e6ccd838b1f118d746c4052a075963d94aec00f76308`. It found no defects at the stated scope and did not run binaries. Tool/runtime reuse remains installed-host evidence, not an independently archived complete tool capsule or new-machine proof. The private receipt route is maintained by the existing foundation owner; it is not an accessible public dependency in this source repository.

Handbook 013 incorporated the earlier accepted recovery guide. The November 4 foundation closure cites Handbook 020. Foundation maintains the exact source and file pins for each projection. [Issue 6](https://github.com/Limited-Underground/TrackMyClicks/issues/6)

## A future authorized reconstruction

Start in a new empty task directory, obtain the exact approved source/ref and declare SDK, reference packs, runtime/native SQLite, caches, feeds and executor inputs. The product source has no global.json or dependency lock; the accepted run pinned its own SDK 10.0.303 with rollForward disabled. Do not qualify through hidden original-checkout caches, prior outputs, private fixtures or an owner's profile. Core needs the .NET reference pack; desktop builds additionally need Windows Desktop packs. Use [Build](BUILD.md) to distinguish those test boundaries.

The public Core suites create random new synthetic roots and accept no restored-profile path. The tested external executor must be supplied separately at its hash above; it is compiled/tested but not remotely distributed here. Without that source/project and declared tool closure, a public reader cannot reproduce the extra restore rehearsal solely from this repository. That is an explicit supply gap, not permission to replace the executor with a tracker launch.

Generate only synthetic schema 0 data, take a coherent backup, hash it and use CreateNew semantics to a second empty fixture. Compare all seeded aggregates/settings/earned IDs. For generated supported schema 1, copy only a confirmed closed store with no nonempty WAL and compare complete persisted expansion JSON/reopen behavior. Preserve source/failure files; never overwrite or open owner data. A casually copied live SQLite main file is not a coherent backup.

The external seed has zero earned challenge rewards; once-only nonzero reward persistence was exercised separately in the46 expansion assertions. Do not overstate one fixture as complete real-data, cross-version or semantic-state coverage. Supported-Version JSON with omitted collections may be admitted as defaults; successful admission is not completeness proof. Preserve/quarantine unsupported, incomplete or uncertain state before any future executor; no speculative repair/reset is authorized.

## Historical packaging and remaining gates

Evaluation ZIPs used an app-relative bundled runtime after self-contained restore could not obtain matching packs. Keep original manifests and exact host/shared runtime notices. Rebuilding with a different SDK/runtime may work without byte equality. No package launch, original-binary equality attempt, owner-data recovery, native usability/CPU qualification or public release is newly claimed. See [Status](PROJECT_STATUS.md#remaining-v1-readiness-gaps) and [Release](RELEASING.md).

The prior independent product-doc reviewer closed three wording findings, not product defects. Official model syntax/exports passed; [six inspection findings](architecture/README.md#retained-inspection-findings) remain. None implies full architecture/ISO acceptance or lifts the freeze.
