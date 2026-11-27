## Releases

### 1.0.0

- Renamed from `Anarchy.Toolkit.Web.MockServer.Ui`, whose last release was 2.1.0; the releases below were published under that name.
- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.

### 2.1.0

- Update the persistence-abstractions dependency.

### 2.0.1

- fix(Web): Support Windows paths and host architecture
- chore(build): Format package release notes property

### 2.0.0

- feat(automation): establish onboarding and layering conventions
- feat(telemetry)!: introduce provider-neutral application telemetry
- refactor(packages)!: restructure persistence and automation packages
- refactor(infrastructure.correlationid)!: decouple correlation scopes from Serilog
- refactor(core)!: reshape telemetry, event and health contracts
- fix(persistence)!: preserve unit-of-work transaction ownership
- refactor(telemetry)!: remove legacy APIs and simplify namespaces
- feat(automation)!: configure layering and bridge build telemetry
- refactor(telemetry)!: update consumers and samples
- docs(onboarding): align package guidance, skills, and architecture

### 1.0.0

- feat(Web.MockServer.Ui): add a browser UI for the mock server with expectations, requests, middlewares and live logs
