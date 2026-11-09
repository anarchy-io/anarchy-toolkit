# Anarchy.Toolkit.Core.Health

Shared health-check contracts and report models for service endpoints.

```bash
dotnet add package Anarchy.Toolkit.Core.Health
```

- Implement `IServiceHealthReport` or derive from `ServiceHealthReportBase` for timed, exception-safe checks.
- Use `HealthCheckReport.RunAsync(reports)` to run reports in parallel and aggregate their results.
- `HealthCheckEntity`, `ExceptionReport`, `IBuildMetadata` and `ApplicationRunbookUriAttribute` provide shared health and status metadata.

## When to use it

Use this package to describe dependency checks independently of the HTTP host. It is suitable for checks such as a database ping or a downstream service probe; the web and worker packages can expose the results using the same model.

## How to use it

Implement a report for each dependency. Return normally for a passing check and throw on failure; the base class records the duration and converts exceptions into failed results. Caller-requested cancellation is propagated.

```csharp
using Anarchy.Toolkit.Core.Health.Service.Health;

public sealed class ExampleHealthReport : ServiceHealthReportBase
{
    protected override string Name => "example_dependency";

    protected override async Task CheckAsync(CancellationToken cancellationToken)
    {
        // Replace with a real, bounded dependency check.
        await Task.Delay(1, cancellationToken);
    }
}
```

Supply reports as an `IReadOnlyCollection<IServiceHealthReport>` to the toolkit's web or worker health endpoints, or call `HealthCheckReport.RunAsync(reports)` to aggregate them directly. A successful placeholder check does not establish that a real dependency is healthy.

The [Web](https://www.nuget.org/packages/Anarchy.Toolkit.Web) and [Hosting.Workers](https://www.nuget.org/packages/Anarchy.Toolkit.Hosting.Workers) packages expose health endpoints. See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced health check entities and reports, build metadata and health reporting abstractions.
