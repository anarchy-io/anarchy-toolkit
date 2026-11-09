# Anarchy.Toolkit.Core.Observability

Observability primitives centered on the `Observer` facade for logging, metrics and context.

```bash
dotnet add package Anarchy.Toolkit.Core.Observability
```

- Use `Observer` with its `Logger`, `MeasurerBase` and `LogContext` to instrument application code.
- Attach `Tags` and call-site information with `CallSiteEntity`, `CalledFrom()` and `ParentCaller()`.
- `DevelopmentLogger` and `LoggerMeasurer` provide development-friendly implementations.

## When to use it

Use `Observer` in services and adapters that need consistent structured logs and metrics without choosing an organization-specific telemetry backend. It wraps Serilog logging and a measurement abstraction; the host still needs to configure their implementations and destinations.

## How to use it

Inject an `Observer` into the component being instrumented. Prefer call-site-aware wrappers and structured properties:

```csharp
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

var caller = this.CalledFrom();
observer.LogInformation(caller, "Processing order {OrderId}", orderId);
observer.ExecutionIncrementCount(caller, 1, Tags.Success);
```

Use `observer.MonitoredActionAsync` for an async action that should report duration and outcome. Keep high-cardinality values such as IDs in logs rather than metric tags. `Configuration` supplies a host-level `ObserverFactory`; `Testing` supplies an in-memory observer for assertions.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to NUKE

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced the observer facade, structured logging context, call-site capture and measurement primitives.
