using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Reports;
using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Core.Health.Checks;

/// <summary>
/// Stores the result of a service's health-check
/// </summary>
/// <remarks>
/// Usually produced by <see cref="HealthCheckBase"/> and aggregated in a
/// <see cref="HealthCheckReport"/>.
/// </remarks>
/// <param name="Name">The name of the test, a name that is meaningful to people supporting the application</param>
/// <param name="Result">
/// The state of the test, may be not_run, running, passed, failed (the descriptions of <see cref="HealthCheckResult"/>)
/// </param>
/// <param name="TestedAt">The time at which this test was executed</param>
/// <param name="Duration">Time taken to run the test</param>
/// <param name="Problem">Optional error when test has failed</param>
public record HealthCheckOutcome(
    string Name,
    string Result,
    DateTimeOffset TestedAt,
    TimeSpan Duration,
    ExceptionReport? Problem = null)
{
    /// <summary>
    /// Creates a result stamped with the current UTC time, converting <paramref name="result"/> to its description
    /// and <paramref name="exception"/> to an <see cref="ExceptionReport"/>.
    /// </summary>
    /// <param name="name">The name of the test.</param>
    /// <param name="result">The outcome of the test.</param>
    /// <param name="duration">Time taken to run the test.</param>
    /// <param name="exception">The failure, if any.</param>
    public HealthCheckOutcome(string name,
                             HealthCheckResult result,
                             TimeSpan duration,
                             Exception? exception = null) : this(name, result.ToDescription(), DateHelper.Now(),
        duration, exception is null ? null : new ExceptionReport(exception))
    { }
}
