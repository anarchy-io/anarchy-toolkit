using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Entity.Api.Output;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Core.Health.Entity.Service;

/// <summary>
/// Stores the result of a service's health-check
/// </summary>
/// <param name="Name">The name of the test, a name that is meaningful to people supporting the application</param>
/// <param name="Result">The state of the test, may be not_run, running, passed, failed</param>
/// <param name="TestedAt">The time at which this test was executed</param>
/// <param name="Duration">Number of milliseconds taken to run the test</param>
/// <param name="Problem">Optional error when test has failed</param>
public record HealthCheckEntity(
    string Name,
    string Result,
    DateTimeOffset TestedAt,
    TimeSpan Duration,
    ExceptionReport? Problem = null)
{
    public HealthCheckEntity(string name,
                             HealthCheckResult result,
                             TimeSpan duration,
                             Exception? exception = null) : this(name, result.ToDescription(), DateHelper.Now(),
        duration, exception is null ? null : new ExceptionReport(exception))
    { }
}
