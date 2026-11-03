# Contributing to TrackMyClicks

**Current state — November 3, 2026.** The November 2 product-development freeze remains in force. Current authorization is a light documentation reconciliation under [issue 6](https://github.com/Limited-Underground/TrackMyClicks/issues/6); no build/recovery replay, App/hooks, profile inspection or product behavior edits. Read [Foundation](docs/FOUNDATION.md), [Status](docs/PROJECT_STATUS.md) and [Recovery](docs/RECOVERY.md). The completed, accepted Core-only recovery is evidence to reuse, not a recurring execution grant. Preserve public baseline 60e0a751 separately from private uncommitted overlays. Fresh scoped review and explicit publication/release authorization remain prerequisites to any commit/push/PR/merge in this assignment.

Contributions are welcome under Apache License 2.0. Intentionally submitted contributions follow LICENSE; no additional contributor agreement is required. Retain existing copyright and third-party notices. Mark modified files appropriately and describe modifications in the pull request.

Read README.md and docs/PROJECT_STATUS.md first. The [dedicated Project](https://github.com/orgs/Limited-Underground/projects/17) owns task scope, status, priorities and owner decisions. Reuse an existing issue before adding work; record acceptance criteria and dependencies. Set In Progress while actively working and Done only after the task's acceptance criteria pass. A board status is not permission to publish a release.

Before edits inspect the checkout, branch, dirty work and existing claims. Record the responsible worker and bounded edit scope in the issue. Preserve unrelated work and history. Use focused task branches from main and focused pull requests. Explain the resulting behavior, validation and limits. Obtain independent review for consequential privacy, persistence or publication changes; human release acceptance is separate.

Use C#/.NET 10 with nullable checking. Keep input callbacks small and memory-only; perform persistence and UI work outside hooks. Preserve local-only aggregate storage, visible pause, migration backups and once-only rewards. Do not add network activity or persist key identities, content, application names or window titles. Discuss new dependencies and their licenses before adoption.

When separately authorized, build and test using [Build](docs/BUILD.md); Core-only and desktop/hook suites have different prerequisites. Use synthetic disposable profiles, never an owner's database. Update the existing status entry and affected instructions at handoff with source identity, verified outcomes, limitations, blockers and next action. Keep private logs, paths and databases outside Git. Report security problems using SECURITY.md.

Publication must use a reviewed explicit file set and inspect committed history as well as the current tree. Never add a whole prototype folder blindly. No force pushes, unrelated publication or bypass of required checks. Release procedure is docs/RELEASING.md.

Housekeeping is part of the task: remove only verified disposable task-owned output; preserve evidence, earlier artifacts and dirty source. Report temporary files removed, retained files/purpose, worktree status, evidence preserved and cleanup requiring approval. No cleanup by age or broad project-root deletion.
