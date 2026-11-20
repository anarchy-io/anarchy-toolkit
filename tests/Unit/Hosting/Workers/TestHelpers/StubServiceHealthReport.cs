using Anarchy.Toolkit.Core.Health.Checks;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;

/// <summary>
/// A service health report that always returns the same result.
/// </summary>
public class StubServiceHealthReport(string name, HealthCheckResult result = HealthCheckResult.Passed)
    : IHealthCheck
{
    public Task<HealthCheckOutcome> RunAsync(CancellationToken? cancellationToken = null) =>
        Task.FromResult(new HealthCheckOutcome(name, result, TimeSpan.Zero));
}
