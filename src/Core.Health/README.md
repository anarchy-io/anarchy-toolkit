# Anarchy.Toolkit.Core.Health

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Core · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Shared health-check contracts and report models for service endpoints.

```bash
dotnet add package Anarchy.Toolkit.Core.Health
```

- Implement `IHealthCheck` or derive from `HealthCheckBase` for timed, exception-safe checks.
- Use `HealthCheckReport.RunAsync(reports)` to run reports in parallel and aggregate their results.
- `HealthCheckOutcome`, `ExceptionReport`, `IBuildMetadata` and `ApplicationRunbookUriAttribute` provide shared health and status metadata.

## When to use it

Use this package to describe dependency checks independently of the HTTP host. It is suitable for checks such as a database ping or a downstream service probe; the web and worker packages can expose the results using the same model.

## How to use it

Implement a report for each dependency. Return normally for a passing check and throw on failure; the base class records the duration and converts exceptions into failed results. Caller-requested cancellation is propagated.

```csharp
using Anarchy.Toolkit.Core.Health.Checks;

public sealed class ExampleHealthReport : HealthCheckBase
{
    protected override string Name => "example_dependency";

    protected override async Task CheckAsync(CancellationToken cancellationToken)
    {
        // Replace with a real, bounded dependency check.
        await Task.Delay(1, cancellationToken);
    }
}
```

Supply reports as an `IReadOnlyCollection<IHealthCheck>` to the toolkit's web or worker health endpoints, or call `HealthCheckReport.RunAsync(reports)` to aggregate them directly. A successful placeholder check does not establish that a real dependency is healthy.

The [Web](https://www.nuget.org/packages/Anarchy.Toolkit.Web) and [Hosting.Workers](https://www.nuget.org/packages/Anarchy.Toolkit.Hosting.Workers) packages expose health endpoints. See the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

### 1.1.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.1.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.0.6

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.5

- refactor(src): clean up code style across library packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced health check entities and reports, build metadata and health reporting abstractions.
