# Anarchy.Toolkit.Core.Telemetry

Provider-neutral application telemetry built on Microsoft logging and `System.Diagnostics.Metrics`. The application
chooses and owns the logger and metrics collection pipeline; no Serilog or vendor SDK is required by this package.

`IAppTelemetry` provides structured logs and scopes, call-site names, counters, gauges, histograms, durations,
text events and monitored operations. Text events are logged rather than converted to numeric metric labels. Configure
`ILoggerFactory` and `IMeterFactory` in dependency injection (for example with `AddLogging()` and `AddMetrics()`), then
call `services.AddAppTelemetry()`. Registration does
not replace an existing `IAppTelemetry`. A custom implementation can also be registered by the host. The facade does
not dispose externally supplied loggers or meters.

The meter is named `Anarchy.Toolkit.Telemetry`. Configure a listener such as OpenTelemetry to collect it and route it
to the host's chosen backend. A hosted application can opt into log-based metric output with
`services.AddTelemetryLogMetrics()`: measurements flow through its configured logging providers, without creating an
independent sink, and listeners from separate DI containers only observe their own meters. The existing toolkit packages still use `Core.Observability` until their migration; this
package does not yet redirect those packages' telemetry.

For vendor backends, OpenTelemetry can collect this meter and export it using OTLP. Configure the destination according
to the vendor's instructions: [Datadog's Agent](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest_in_the_agent/)
requires OTLP ingestion to be enabled, while its [direct metrics endpoint](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest/metrics/)
requires HTTP and delta temporality. [New Relic](https://docs.newrelic.com/docs/opentelemetry/best-practices/opentelemetry-otlp/)
requires its endpoint and API key and documents its temporality recommendations. An in-memory OTLP HTTP test covers export
without vendor credentials; it does not verify delivery to either vendor.

## Releases

### 1.0.0

- Introduced a provider-neutral application telemetry facade backed by Microsoft logging and standard .NET metrics.
- Added an opt-in, DI-isolated listener that forwards toolkit metrics to the configured logging pipeline.
- Added standard gauge and histogram recording, structured log contexts and text events.

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
