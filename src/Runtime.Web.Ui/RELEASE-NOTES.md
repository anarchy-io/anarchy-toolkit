## Releases

### 1.0.0

- Renamed from `Anarchy.Toolkit.Web.Ui`, whose last release was 3.1.0; the releases below were published under that name.
- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.

### 3.1.0

- Update the persistence-abstractions dependency.

### 3.0.1

- fix(Web): Support Windows paths and host architecture
- chore(build): Format package release notes property

### 3.0.0

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

### 2.0.0

- refactor(Web.Ui)!: make the cookie session an opt-in add-on and allow UIs without a startup base

### 1.2.2

- fix(Web.Ui): serve static web assets of unpublished builds outside Development

### 1.2.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.2.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1
- feat(Web): serve OpenAPI 3.2 and theme Swagger UI with a light and dark Anarchy theme
- feat(Web.Ui): align the UI theme with the documentation and add dark mode

### 1.0.8

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.7

- References `Anarchy.Toolkit.Web` 1.0.7, aligned with the .NET 10 LTS.

### 1.0.6

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

### 1.0.5

- chore(deps): update AWS SDK packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced Blazor UI hosting, session handling, presentation services, HTTP client infrastructure and reusable components.
