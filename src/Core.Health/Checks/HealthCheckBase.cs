using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Health.Checks;

/// <summary>
/// Base <see cref="IHealthCheck"/> that times the check and turns any exception it throws into a
/// <see cref="HealthCheckResult.Failed"/> result carrying the exception. Cancellation requested by the caller
/// is rethrown instead.
/// </summary>
/// <example>
/// <code language="csharp">
/// public sealed class DatabaseHealthReport(IDbConnectionFactory factory) : HealthCheckBase
/// {
///     protected override string Name =&gt; "Database connectivity";
///
///     protected override async Task CheckAsync(CancellationToken cancellationToken)
///     {
///         await using var connection = await factory.OpenAsync(cancellationToken);
///     }
/// }
/// </code>
/// </example>
public abstract class HealthCheckBase : IHealthCheck
{
    /// <summary>
    /// The name of the test, meaningful to people supporting the application.
    /// </summary>
    protected abstract string Name { get; }

    /// <summary>
    /// Checks the dependency. Return to pass, throw to fail.
    /// </summary>
    /// <param name="cancellationToken">
    /// The caller's token, or <see cref="CancellationToken.None"/> when none was provided.
    /// </param>
    /// <returns>A task that completes when the check succeeds.</returns>
    protected abstract Task CheckAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">
    /// The check was cancelled through <paramref name="cancellationToken"/>.
    /// </exception>
    public async Task<HealthCheckOutcome> RunAsync(CancellationToken? cancellationToken = null)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            await CheckAsync(cancellationToken ?? CancellationToken.None);

            return new HealthCheckOutcome(Name, HealthCheckResult.Passed, timer.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken?.IsCancellationRequested == true)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new HealthCheckOutcome(Name, HealthCheckResult.Failed, timer.Elapsed, exception);
        }
    }
}
