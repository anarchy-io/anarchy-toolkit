using Anarchy.Toolkit.Build.Cli.Core.Service;
using Fallout.Common;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Build.Fallout.Component;

public partial interface IStandardBuild
{
    /// <summary>
    /// Restores the NuGet packages of the solution.
    /// </summary>
    Target Restore => d => d
        .Description("Installs NuGet packages")
        .Executes(() => DotNetRestore(s => s
            .SetProjectFile(Solution)
            .SetVerbosity(ToolVerbosity)));

    /// <summary>
    /// Builds the solution in <see cref="IDefaultBuild.Configuration"/>, after <see cref="Restore"/>, honouring
    /// <see cref="IDefaultBuild.TreatWarningsAsErrors"/>.
    /// </summary>
    Target Compile => d => d
        .Description("Compiles the full solution")
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(s => s
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration)
            .SetNoRestore(IsRestored)
            .SetVerbosity(ToolVerbosity)
            .SetTreatWarningsAsErrors(TreatWarningsAsErrors)));

    /// <summary>
    /// Runs the <c>Unit</c> test projects (see <see cref="IsTestProjectOfKind"/>) after <see cref="Compile"/>.
    /// </summary>
    Target TestUnit => d => d
        .Description("Runs test projects: Unit")
        .DependsOn(Compile)
        .Executes(() => RunTestsOfKind(TestKind.Unit));

    /// <summary>
    /// Runs the <c>Integration</c> test projects (see <see cref="IsTestProjectOfKind"/>) after <see cref="Compile"/>.
    /// </summary>
    Target TestIntegration => d => d
        .Description("Runs test projects: Integration")
        .DependsOn(Compile)
        .Executes(() => RunTestsOfKind(TestKind.Integration));

    /// <summary>
    /// Runs the <c>EndToEnd</c> test projects (see <see cref="IsTestProjectOfKind"/>) after <see cref="Compile"/>.
    /// </summary>
    Target TestEndToEnd => d => d
        .Description("Runs test projects: EndToEnd")
        .DependsOn(Compile)
        .Executes(() => RunTestsOfKind(TestKind.EndToEnd));

    /// <summary>
    /// Runs <see cref="TestUnit"/>, <see cref="TestIntegration"/> and <see cref="TestEndToEnd"/>.
    /// </summary>
    Target TestAll => d => d
        .Description("Runs all solution tests")
        .DependsOn(TestUnit, TestIntegration, TestEndToEnd);

    /// <summary>
    /// Verifies with <c>dotnet format --verify-no-changes</c> that the solution needs no formatting or analyzer fixes of
    /// warning severity or above, failing otherwise.
    /// </summary>
    Target Lint => d => d
        .Description("Runs linter analysis on the whole solution")
        .DependsOn(Restore)
        .Executes(() => DotNet("format " + LintFlags() + " --verify-no-changes", RootDirectory));

    /// <summary>
    /// Applies <c>dotnet format</c> fixes of warning severity or above, re-staging them when
    /// <see cref="AutoStageLintFixes"/> is set.
    /// </summary>
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
    /// <returns>The options, space-separated.</returns>
    string LintFlags()
    {
        var result = new List<string> { "--severity", "warn", "--verbosity", ToolVerbosity.ToString() };
        if (IsRestored) result.Add("--no-restore");

        return result.Join(" ");
    }
}
