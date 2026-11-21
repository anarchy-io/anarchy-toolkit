# Anarchy.Toolkit.Infrastructure.Telemetry.Serilog

Optional Serilog integration for `Anarchy.Toolkit.Core.Telemetry`. Serilog becomes one more Microsoft logging
provider: the toolkit's instrumentation code does not change, and Core keeps no Serilog dependency.

```csharp
services.AddSerilogTelemetry(logger);                    // application-owned logger, never disposed here
services.AddSerilogTelemetry(config => config.WriteTo.Console()); // logger owned and flushed by the container
```

- Registration does not clear other logging providers, registers a given logger once, and keeps existing
  `ICoreTelemetry`, `IMetricRecorder` or `AppTelemetry` registrations (it ends with `AddCoreTelemetry()`).
  `ModuleSerilogTelemetry` is the same registration as a `ModuleTelemetryBase`.
- `SerilogConsoleDefaults` gives composition roots that own their output (command-line tools) a readable console
  logger; `AddSerilogConsoleTelemetry()` adds it only when no logging provider is registered. Libraries never add it.
- `SerilogTelemetry.Create(logger)` builds an `AppTelemetry` over a Serilog logger without DI.
- `Enrich.WithCorrelationId(correlationIdPropertyName, attemptIdPropertyName)` is an opt-in enricher that reads the
  ambient `CorrelationContext`. It adds each property only when the event does not already have it
  (`AddPropertyIfAbsent`), so it never duplicates the properties a Serilog provider already took from the
  `ILogger.BeginScope` scope that `Infrastructure.CorrelationId` opens. Use it for events written outside such a scope
  (code that sets `CorrelationContext.SetCorrelationValues` directly); the property names default to those of the
  correlation package and must match what the scopes use.
- `Enrich.WithStructuredExceptionContext()` adds a structured `Exceptions` property to events with an exception.
- `AddTelemetryLogMetrics()` opts into log-based metrics: it decorates the registered `IMetricRecorder` with a
  `LogMetricRecorder` that also writes each metric as a log event (category `Anarchy.Toolkit.Telemetry.Metrics`)
  through the application's logging pipeline, for log backends that derive metrics from logs.

Metrics otherwise use the application's standard .NET metrics pipeline. This package has no AWS or vendor
dependency.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
