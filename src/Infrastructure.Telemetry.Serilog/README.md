# Anarchy.Toolkit.Infrastructure.Telemetry.Serilog

Optional Serilog integration for `Anarchy.Toolkit.Core.Telemetry`. Supply an already-configured Serilog logger and
its sinks through the composition root:

```csharp
services.AddMetrics();
services.AddSerilogTelemetry(logger);
```

The logger remains owned by the application. Registration does not clear other logging providers or replace an
existing `IAppTelemetry` registration. Metrics use the application's standard .NET metrics pipeline independently of
Serilog. `ModuleSerilogTelemetry` wraps the same registration for composition roots using toolkit DI modules. This
general-purpose package does not require AWS or CloudWatch; those integrations remain separate.

The existing toolkit packages still use `Core.Observability` until their telemetry migration is complete.

## Releases

### 1.0.0

- Added optional DI integration for a consumer-configured Serilog logger and the provider-neutral telemetry facade.
