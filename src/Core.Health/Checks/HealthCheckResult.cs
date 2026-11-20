using System.ComponentModel;

namespace Anarchy.Toolkit.Core.Health.Checks;

/// <summary>
/// The state of a health-check test. The <see cref="DescriptionAttribute"/> text is the value exposed in
/// <see cref="HealthCheckOutcome.Result"/>.
/// </summary>
public enum HealthCheckResult
{
    /// <summary>The test succeeded (<c>passed</c>).</summary>
    [Description("passed")] Passed,

    /// <summary>The test failed (<c>failed</c>).</summary>
    [Description("failed")] Failed,

    /// <summary>The test has not been executed (<c>not_run</c>).</summary>
    [Description("not_run")] NotRun,

    /// <summary>The test is in progress (<c>running</c>).</summary>
    [Description("running")] Running,
}
