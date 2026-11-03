using System.ComponentModel;

namespace Anarchy.Toolkit.Core.Health.ValueSet.Service;

public enum HealthCheckResult
{
    [Description("passed")] Passed,
    [Description("failed")] Failed,
    [Description("not_run")] NotRun,
    [Description("running")] Running,
}
