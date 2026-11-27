## Releases

### 3.0.0

- **Breaking:** the MSAL certificate authentication handler, its settings and exceptions move to the new opt-in `Anarchy.Toolkit.Infrastructure.Http.Msal`, so this package no longer depends on Microsoft.Identity.Client.
- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.

### 2.0.1

- chore(build): Format package release notes property

### 2.0.0

- Replace the unused `Observer` constructor argument with `AppTelemetry` and monitor HTTP operations through the host's telemetry pipeline.

### 1.1.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.1.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.0.6

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.5

- refactor(src): clean up code style across library packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced HTTP repository bases, client configuration, authentication and timeout handlers, and request and response helpers.
