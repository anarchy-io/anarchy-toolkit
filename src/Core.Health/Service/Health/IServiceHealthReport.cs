using Anarchy.Toolkit.Core.Health.Entity.Service;

namespace Anarchy.Toolkit.Core.Health.Service.Health;

/// <summary>
/// A single health check of a service dependency (database, broker, external API...).
/// </summary>
/// <remarks>
/// Implementations are typically registered in DI and run together through
/// <see cref="Entity.Api.Output.HealthCheckReport.RunAsync"/>. Derive from <see cref="ServiceHealthReportBase"/> to
/// get timing and exception handling for free.
/// </remarks>
public interface IServiceHealthReport
{
    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The outcome of the check.</returns>
    public Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null);
}
