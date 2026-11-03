using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Health.Entity.Api.Output;

public record HealthCheckReport(bool Passed, TimeSpan Duration, IReadOnlyCollection<HealthCheckEntity> Tests)
{
    public HealthCheckReport(TimeSpan Duration, IReadOnlyCollection<HealthCheckEntity> Tests) : this(
        Tests.All(x => x.Result != HealthCheckResult.Failed.ToDescription()), Duration, Tests)
    { }

    /// <summary>
    /// Runs every report in parallel and aggregates the results. The report passes when no test failed,
    /// including when there are no reports at all.
    /// </summary>
    public static async Task<HealthCheckReport> RunAsync(IEnumerable<IServiceHealthReport> reports,
                                                         CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var timer = Stopwatch.StartNew();
        var tests = await Task.WhenAll(reports.Select(report => report.RunAsync(cancellationToken)));

        return new HealthCheckReport(timer.Elapsed, tests);
    }
}
