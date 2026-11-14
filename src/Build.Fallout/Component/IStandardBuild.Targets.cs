using Anarchy.Toolkit.Build.Cli.Core.Service;
using Fallout.Common;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Build.Fallout.Component;

public partial interface IStandardBuild
{
    Target Restore => d => d
        .Description("Installs NuGet packages")
        .Executes(() => DotNetRestore(s => s
            .SetProjectFile(Solution)
            .SetVerbosity(ToolVerbosity)));

    Target Compile => d => d
        .Description("Compiles the full solution")
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(s => s
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration)
            .SetNoRestore(IsRestored)
            .SetVerbosity(ToolVerbosity)
            .SetTreatWarningsAsErrors(TreatWarningsAsErrors)));

    Target TestUnit => d => d
        .Description("Runs test projects: Unit")
        .DependsOn(Compile)
        .Executes(() => RunTestsOfKind(TestKind.Unit));

    Target TestIntegration => d => d
        .Description("Runs test projects: Integration")
        .DependsOn(Compile)
        .Executes(() => RunTestsOfKind(TestKind.Integration));

    Target TestEndToEnd => d => d
        .Description("Runs test projects: EndToEnd")
        .DependsOn(Compile)
        .Executes(() => RunTestsOfKind(TestKind.EndToEnd));

    Target TestAll => d => d
        .Description("Runs all solution tests")
        .DependsOn(TestUnit, TestIntegration, TestEndToEnd);

    Target Lint => d => d
        .Description("Runs linter analysis on the whole solution")
        .DependsOn(Restore)
        .Executes(() => DotNet("format " + LintFlags() + " --verify-no-changes", RootDirectory));

    Target LintFix => d => d
        .Description("Runs the linter auto-fixer on the whole solution")
        .DependsOn(Restore)
        .Executes(() => LintStagingTasks.Run(RootDirectory, AutoStageLintFixes, () => DotNet("format " + LintFlags(), RootDirectory)));

    /// <summary>
    /// Validates a pull request: lints, checks the layering (when the build implements
    /// <see cref="ILayeringBuild"/>) and runs every kind of test.
    /// </summary>
    Target PullRequest => d => d
        .Description("Runs all tests and validations for the Pull Request CI-pipeline")
        .DependsOn(Lint)
        .TryDependsOn<ILayeringBuild>(x => x.CheckLayering)
        .DependsOn(TestAll);

    /// <summary>
    /// The <c>dotnet format</c> options shared by <see cref="Lint"/> and <see cref="LintFix"/>.
    /// </summary>
    string LintFlags()
    {
        var result = new List<string> { "--severity", "warn", "--verbosity", ToolVerbosity.ToString() };
        if (IsRestored) result.Add("--no-restore");

        return result.Join(" ");
    }
}
