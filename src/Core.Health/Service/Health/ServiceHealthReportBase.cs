using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Health.Service.Health;

/// <summary>
/// Base <see cref="IServiceHealthReport"/> that times the check and turns any exception it throws into a
/// <see cref="HealthCheckResult.Failed"/> result carrying the exception. Cancellation requested by the caller
/// is rethrown instead.
/// </summary>
public abstract class ServiceHealthReportBase : IServiceHealthReport
{
    /// <summary>
    /// The name of the test, meaningful to people supporting the application.
    /// </summary>
    protected abstract string Name { get; }

    /// <summary>
    /// Checks the dependency. Return to pass, throw to fail.
    /// </summary>
    protected abstract Task CheckAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            await CheckAsync(cancellationToken ?? CancellationToken.None);

            return new HealthCheckEntity(Name, HealthCheckResult.Passed, timer.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken?.IsCancellationRequested == true)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new HealthCheckEntity(Name, HealthCheckResult.Failed, timer.Elapsed, exception);
        }
    }
}
