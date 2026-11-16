using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Health.Entity.Api.Output;

/// <summary>
/// Aggregated outcome of running a set of <see cref="IServiceHealthReport"/>s, suitable as a health endpoint
/// response.
/// </summary>
/// <param name="Passed">Whether every test passed (no test has the <c>failed</c> result).</param>
/// <param name="Duration">The total time taken to run all tests.</param>
/// <param name="Tests">The individual test results.</param>
/// <example>
/// <code language="csharp">
/// app.MapGet("/health", async (IEnumerable&lt;IServiceHealthReport&gt; reports, CancellationToken ct) =&gt;
/// {
///     var report = await HealthCheckReport.RunAsync(reports, ct);
///     return report.Passed ? Results.Ok(report) : Results.Json(report, statusCode: 503);
/// });
/// </code>
/// </example>
public record HealthCheckReport(bool Passed, TimeSpan Duration, IReadOnlyCollection<HealthCheckEntity> Tests)
{
    /// <summary>
    /// Creates a report, computing <see cref="Passed"/> as "no test has the <see cref="HealthCheckResult.Failed"/>
    /// result".
    /// </summary>
    /// <param name="Duration">The total time taken to run all tests.</param>
    /// <param name="Tests">The individual test results.</param>
    public HealthCheckReport(TimeSpan Duration, IReadOnlyCollection<HealthCheckEntity> Tests) : this(
        Tests.All(x => x.Result != HealthCheckResult.Failed.ToDescription()), Duration, Tests)
    { }

    /// <summary>
    /// Runs every report in parallel and aggregates the results. The report passes when no test failed,
    /// including when there are no reports at all.
    /// </summary>
    /// <param name="reports">The health checks to run.</param>
    /// <param name="cancellationToken">Optional cancellation token forwarded to every check.</param>
    /// <returns>The aggregated report.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reports"/> is <see langword="null"/>.</exception>
    public static async Task<HealthCheckReport> RunAsync(IEnumerable<IServiceHealthReport> reports,
                                                         CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var timer = Stopwatch.StartNew();
        var tests = await Task.WhenAll(reports.Select(report => report.RunAsync(cancellationToken)));

        return new HealthCheckReport(timer.Elapsed, tests);
    }
}
