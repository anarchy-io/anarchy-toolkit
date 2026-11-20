namespace Anarchy.Toolkit.Automation.Build.Cli.Command;

/// <summary>The kind of project requested by the <c>scaffold</c> command.</summary>
public enum ScaffoldTarget
{
    /// <summary>An API, job or UI application.</summary>
    Application,

    /// <summary>A named library project within a solution layer.</summary>
    Library,

    /// <summary>The shared test project for one test kind.</summary>
    Tests
}
