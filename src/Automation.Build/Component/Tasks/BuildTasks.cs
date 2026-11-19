using Fallout.Common.Tools.DotNet;

namespace Anarchy.Toolkit.Automation.Build.Component.Tasks;

/// <summary>
/// Tasks related to building solutions.
/// </summary>
public static class BuildTasks
{
    /// <summary>
    /// Runs <c>dotnet build</c> on the solution of the build specified, using its <see cref="IDefaultBuild.Configuration"/>,
    /// <see cref="IDefaultBuild.ToolVerbosity"/> and <see cref="IDefaultBuild.TreatWarningsAsErrors"/>.
    /// </summary>
    /// <param name="build">The build whose <see cref="IDefaultBuild.Solution"/> to build.</param>
    /// <example>
    /// <code language="csharp">
    /// Target Compile => d => d.Executes(() => this.As&lt;IDefaultBuild&gt;().BuildSolution());
    /// </code>
    /// </example>
    public static void BuildSolution(this IDefaultBuild build) =>
        DotNetTasks.DotNetBuild(
            s => s.SetProjectFile(build.Solution)
                .SetConfiguration(build.Configuration)
                .SetVerbosity(build.ToolVerbosity)
                .SetTreatWarningsAsErrors(build.TreatWarningsAsErrors)
        );
}
