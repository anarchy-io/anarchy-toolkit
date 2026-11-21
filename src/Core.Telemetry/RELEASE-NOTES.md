## Releases

### 1.0.0

- Introduced `ICoreTelemetry` (Microsoft logger and metric recorder), the provider-neutral `IMetricRecorder` with its
  standard .NET metrics implementation, and `AppTelemetry` with call-site attribution, standard metric names, timers,
  monitored actions, log helpers and structured log contexts.
- Added `AddCoreTelemetry()`, `ModuleTelemetryBase` and a non-DI construction path.

### Earlier releases under Anarchy.Toolkit.Core.Observability

The following versions were published under the old package name, not under `Core.Telemetry`.

#### 1.1.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

#### 1.1.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

#### 1.0.6

- docs: expand XML docblocks across all projects for the DocFX API reference

#### 1.0.5

- refactor(src): clean up code style across library packages

#### 1.0.3

- build: migrate local automation to build tooling

#### 1.0.2

- docs: expand package guides and release histories

#### 1.0.1

- Added a package-specific NuGet README.

#### 1.0.0

- Introduced the observer facade, structured logging context, call-site capture and measurement primitives.
