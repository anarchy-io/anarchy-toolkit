## Releases

### 3.0.0

- Breaking: Align event envelope properties with CloudEvents terminology, including `AggregateType`, `Subject`, `Type`, `Time`, and `Data`.
- Preserve custom event attributes across serialization, cloning, and broker-neutral message conversions.

### 2.0.1

- chore(build): Format package release notes property

### 2.0.0

- Local event bus handler failures use the injected `AppTelemetry` instead of `Observer`.
- Domain event processing uses the same facade for event scopes, logs and metrics, removing its Serilog-based dependency.

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

- Introduced domain event envelopes, handlers and processing, the local event bus and message broker envelope parsing.
