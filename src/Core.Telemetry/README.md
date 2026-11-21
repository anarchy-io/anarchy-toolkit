# Anarchy.Toolkit.Core.Telemetry

Provider-neutral telemetry built on Microsoft logging and `System.Diagnostics.Metrics`. The application chooses and
owns the logging and metrics pipeline; no Serilog or vendor SDK is required by this package.

```bash
dotnet add package Anarchy.Toolkit.Core.Telemetry
```

- `ICoreTelemetry` holds the two primitives: an `ILogger` and an `IMetricRecorder`. The default `CoreTelemetry` takes
  them from the application's `ILoggerFactory` and `IMeterFactory`.
- `IMetricRecorder` records counters, gauges, histograms and durations (in seconds) with tags. The default
  `MeterMetricRecorder` creates each instrument once on the `Anarchy.Toolkit.Telemetry` meter, so OpenTelemetry collects
  it with `AddMeter(CoreTelemetry.InstrumentationName)` and no custom code.
- `AppTelemetry` receives `ICoreTelemetry` and holds the toolkit conventions: call-site attribution (`CallSite`,
  `this.CalledFrom()`), standard metric names (`{class_name}.{method_name}.{suffix}`, see `MetricSuffixes`),
  `StartTimer`, `RecordDuration`, `MonitoredActionAsync` (execution count and duration), log helpers and `LogContext`
  scopes. Toolkit packages depend on `AppTelemetry`.

## How to use it

Call `services.AddCoreTelemetry()`. It registers `IMetricRecorder`, `ICoreTelemetry` and `AppTelemetry` with
`TryAdd`, so a registration made before it is kept, and one made after it (or with `services.Replace`) wins. It calls
`AddLogging()` and `AddMetrics()`, which only fill in what is missing; the application's logging providers decide where
logs go. Nothing is disposed by the toolkit types: instances the application registers stay owned by it, and the
logger and meter created by default belong to their factories.

To bring the company's logging and metrics, derive a `ModuleTelemetryBase` and register the providers or a recorder in
`RegisterCoreTelemetry`; the base then completes the registrations other toolkit modules expect. Console tools and
tests that do not use DI call `AppTelemetry.Create(loggerFactory, meterFactory)`; `AppTelemetry.Null` discards
everything.

Notable occurrences are structured log events with a stable `EventId`
(`telemetry.Log(level, callSite, eventId, exception, template, args)`) and, when the count matters, a counter. To send
them to a vendor's event API (Datadog Events, New Relic custom events), add a logging provider or OpenTelemetry
exporter that forwards those events, or decorate the registered `IMetricRecorder`; no vendor package is shipped.

For vendor backends, OpenTelemetry can collect this meter and export it using OTLP. Configure the destination according
to the vendor's instructions: [Datadog's Agent](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest_in_the_agent/)
requires OTLP ingestion to be enabled, while its [direct metrics endpoint](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest/metrics/)
requires HTTP and delta temporality. [New Relic](https://docs.newrelic.com/docs/opentelemetry/best-practices/opentelemetry-otlp/)
requires its endpoint and API key and documents its temporality recommendations. An in-memory OTLP HTTP test covers export
without vendor credentials; it does not verify delivery to either vendor.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
