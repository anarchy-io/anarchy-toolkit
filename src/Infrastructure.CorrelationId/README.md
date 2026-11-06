# Anarchy.Toolkit.Infrastructure.CorrelationId

Correlation and attempt ID propagation for ASP.NET Core services and Serilog.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.CorrelationId
```

- Access IDs through the async-local `CorrelationContext`.
- Add `UseCorrelationId()` to the ASP.NET Core pipeline; headers default to `X-Correlation-Id` and `X-Attempt-Id` and are configurable.
- Add the Serilog `WithCorrelationId()` enricher and register services through `ModuleCorrelationId`.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
