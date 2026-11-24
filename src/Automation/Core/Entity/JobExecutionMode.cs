namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>How a scaffolded job runs locally.</summary>
public enum JobExecutionMode
{
    /// <summary>A long-running worker.</summary>
    Worker,
    /// <summary>Run once and exit.</summary>
    Once
}
