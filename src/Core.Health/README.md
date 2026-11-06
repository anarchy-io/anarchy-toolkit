# Anarchy.Toolkit.Core.Health

Shared health-check contracts and report models for service endpoints.

```bash
dotnet add package Anarchy.Toolkit.Core.Health
```

- Implement `IServiceHealthReport` or derive from `ServiceHealthReportBase` for timed, exception-safe checks.
- Use `HealthCheckReport.RunAsync(reports)` to run reports in parallel and aggregate their results.
- `HealthCheckEntity`, `ExceptionReport`, `IBuildMetadata` and `ApplicationRunbookUriAttribute` provide shared health and status metadata.

The [Web](https://www.nuget.org/packages/Anarchy.Toolkit.Web) and [Hosting.Workers](https://www.nuget.org/packages/Anarchy.Toolkit.Hosting.Workers) packages expose health endpoints. See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
