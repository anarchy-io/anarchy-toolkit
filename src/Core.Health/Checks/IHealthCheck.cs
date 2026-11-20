namespace Anarchy.Toolkit.Core.Health.Checks;

/// <summary>
/// A single health check of a service dependency (database, broker, external API...).
/// </summary>
/// <remarks>
/// Implementations are typically registered in DI and run together through
/// <see cref="Reports.HealthCheckReport.RunAsync"/>. Derive from <see cref="HealthCheckBase"/> to
/// get timing and exception handling for free.
/// </remarks>
public interface IHealthCheck
{
    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The outcome of the check.</returns>
    public Task<HealthCheckOutcome> RunAsync(CancellationToken? cancellationToken = null);
}
