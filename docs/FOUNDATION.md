# Foundation reading path

**Product development remains frozen under the November 2, 2026 owner direction.** Check the maintainer handoff for current authority before tracking launches, builds, profile work, feature changes or release. [Status](PROJECT_STATUS.md)

These documents cover functional baseline 0.3.0 at `60e0a7512033f3ce3af0d52e2545c46c409f8374`. The private overlay preserves performance, tab navigation and tray-first changes from TMC-0002, TMC-0004 and TMC-0005. Overlay integration remains pending.

| Reader need | Owning route |
| --- | --- |
| Purpose and privacy | [README](../README.md) |
| Contributor/agent rules | [Contributing](../CONTRIBUTING.md), [AGENTS](../AGENTS.md) |
| Privacy/vulnerability reporting | [Security](../SECURITY.md) |
| Current freeze, evidence and next action | [Status](PROJECT_STATUS.md), [Project 17](https://github.com/orgs/Limited-Underground/projects/17) |
| Requirements, architecture, rationale and quality gaps | [Twelve arc42 sections](ARCHITECTURE.md), [quality requirements](ARCHITECTURE.md#10-quality-requirements) |
| C4 context/container model and validation limits | [Model](architecture/README.md) |
| Windows input and SQLite contracts | [Interfaces](INTERFACES.md) |
| Toolchain, Core acceptance and separately gated desktop tests | [Build and testing](BUILD.md) |
| Accepted same-host Core recovery, declared inputs and remaining custody gaps | [Recovery](RECOVERY.md) |
| Future binary/publication gates | [Release procedure](RELEASING.md) |

Intended means a requirement; implemented means source inspection at the named identity; demonstrated means executed evidence with its scope; unresolved means no acceptance claim. Documentation/static model checks cannot establish runtime correctness. ISO/IEC/IEEE 42010:2022 is a review reference only; no conformance is claimed.

Keep the original identities and custody records for source, history, packages and evidence. History is linked from current status. Foundation selects handbook inputs through explicit file and hash pins. Keep evidence, profiles, databases, machine records, credentials and packages outside that selection.

The accepted same-host Core recovery completed four Release builds and 106 assertions. Desktop, native input, real-profile and new-machine acceptance remain open. The model guide lists the six inspection findings. [Status](PROJECT_STATUS.md) · [Model](architecture/README.md)
