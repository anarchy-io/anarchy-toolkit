# 0004. `Observer` for logs and metrics

## Context

Logs and metrics written with different APIs drift apart: different names for the same operation, context in some places only, and every package choosing its own metrics client. Organizations also differ in sinks, property names and metrics backends.

## Decision

Toolkit code logs and measures through `Observer` (`Core.Observability`): a [Serilog](https://serilog.net/) logger, a `MeasurerBase` and a log context, with call-site aware helpers that attach class and method to both. The `Observer` is built by an `ObserverFactory` whose steps (enrichers, sinks, measurer, property names) can each be overridden. The default is vendor-neutral: console output, metrics as log events.

## Consequences

- A call site produces logs and metrics with matching context and no naming effort.
- Company specifics live in one factory class, outside the toolkit.
- The toolkit depends on Serilog; applications on [Microsoft.Extensions.Logging](https://learn.microsoft.com/dotnet/core/extensions/logging) can still route both to the same sinks.

## Alternatives considered

- **Microsoft.Extensions.Logging plus [`System.Diagnostics.Metrics`](https://learn.microsoft.com/dotnet/core/diagnostics/metrics) directly.** Standard, but without the call-site conventions and with more setup per backend.
- **A vendor SDK ([Datadog](https://www.datadoghq.com/), New Relic) inside the packages.** Rejected by the [Vendor-neutral value](README.md#values).
