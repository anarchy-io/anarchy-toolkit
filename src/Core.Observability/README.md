# Anarchy.Toolkit.Core.Observability

Observability primitives centered on the `Observer` facade for logging, metrics and context.

```bash
dotnet add package Anarchy.Toolkit.Core.Observability
```

- Use `Observer` with its `Logger`, `MeasurerBase` and `LogContext` to instrument application code.
- Attach `Tags` and call-site information with `CallSiteEntity`, `CalledFrom()` and `ParentCaller()`.
- `DevelopmentLogger` and `LoggerMeasurer` provide development-friendly implementations.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
