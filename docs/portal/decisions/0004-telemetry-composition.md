# 0004. Telemetry composed over the standard .NET APIs

## Context

Logs and metrics written with different APIs drift apart: different names for the same operation, context in some places only, and every package choosing its own metrics client. Organizations also differ in logging providers, property names and metrics backends, and most already run one: the toolkit must feed it rather than ask for a replacement.

## Decision

Toolkit code writes telemetry through `Core.Telemetry`, which is built on the standard .NET APIs and does not depend on Serilog or any vendor SDK:

- **`ICoreTelemetry`** holds the two primitives: a Microsoft `ILogger` and an `IMetricRecorder`. The default implementation takes the logger from the application's `ILoggerFactory`; `IMetricRecorder` is a small provider-neutral contract (counters, gauges, histograms, durations in seconds) whose default records `System.Diagnostics.Metrics` instruments through the application's `IMeterFactory`, so OpenTelemetry collects them and exports them with OTLP without custom code.
- **`AppTelemetry`** is a concrete class that receives `ICoreTelemetry` and holds the toolkit conventions on top: call-site attribution, standard metric names, timers, monitored actions, log helpers and `LogContext` scopes. Toolkit packages depend on it (or on `ICoreTelemetry` when they need only the primitives), so every package emits through the same pipeline.
- **Registration is DI-first.** `AddCoreTelemetry()` uses `TryAdd`, so what the application registered is kept, and anything registered later wins. Applications replace the logging providers, the recorder, `ICoreTelemetry` or `AppTelemetry` through DI; a `ModuleTelemetryBase` packages that choice as a module. Instances the application supplies are never disposed by the toolkit.
- **Serilog is optional.** `Infrastructure.Telemetry.Serilog` registers Serilog as one more logging provider and adds opt-in extras (console defaults for tools, enrichers, log-based metrics). Swapping providers changes only the composition root.
- **Notable events are structured logs, not a vendor API.** A stable `EventId` and properties, plus a counter when the count matters. Routing them to Datadog Events or New Relic custom events is configuration of a provider or exporter.
- **DogStatsD is an optional adapter.** `Infrastructure.Telemetry.StatsD` depends on the DogStatsD client for applications that need that protocol. It is not a dependency of Core, and OTLP remains the preferred default. The application owns the endpoint and settings.
- **Correlation IDs are provider-neutral.** They travel in `ILogger` scopes and `Activity` tags, set by the middleware and by the entry points of workers and message consumers.

## Consequences

- A call site produces logs and metrics with matching context and no naming effort.
- Company specifics live in one module (logging providers, an exporter, optionally a recorder), outside the toolkit.
- Any logging provider and any OpenTelemetry-compatible backend works without changes to instrumentation code.
- The toolkit owns the conventions (names, tags, call sites), not the pipeline; it never creates a private logger or meter that bypasses the application's.
- Metrics follow the standard .NET model, so backend behavior (temporality, units, naming rules) depends on the exporter and the vendor.

## Alternatives considered

- **`ILogger` and `System.Diagnostics.Metrics` directly, with no facade.** Standard, but without the call-site conventions and with the same boilerplate repeated in every package.
- **A Serilog-based facade in Core.** Rejected: it forces one logging library on every adopter. Serilog remains available as an optional provider.
- **A vendor SDK ([Datadog](https://www.datadoghq.com/), New Relic) inside Core or required by other packages.** Rejected by the [Vendor-neutral value](README.md#values): OTLP and providers cover vendors through configuration. A separate opt-in protocol adapter does not impose that dependency on other consumers.
