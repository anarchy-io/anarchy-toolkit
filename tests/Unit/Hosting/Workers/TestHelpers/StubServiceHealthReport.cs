using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;

/// <summary>
/// A service health report that always returns the same result.
/// </summary>
public class StubServiceHealthReport(string name, HealthCheckResult result = HealthCheckResult.Passed)
    : IServiceHealthReport
{
    public Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null) =>
        Task.FromResult(new HealthCheckEntity(name, result, TimeSpan.Zero));
}
