using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    public partial interface IStandardBuild
    {
        Target Restore => d => d
            .Description("Installs NuGet packages")
            .Executes(() => DotNetRestore(s => s.SetProjectFile(Solution)));

        Target Compile => d => d
            .Description("Compiles the full solution")
            .DependsOn(Restore)
            .Executes(() => DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(IsRestored)
                .SetTreatWarningsAsErrors(TreatWarningsAsErrors)));

        Target TestUnit => d => d
            .Description("Runs test projects: Unit")
            .DependsOn(Compile, SetTestEnvironmentVariables)
            .Executes(() => RunTestsOfKind(TestKind.Unit));

        Target TestIntegration => d => d
            .Description("Runs test projects: Integration")
            .DependsOn(Compile, SetTestEnvironmentVariables)
            .Executes(() => RunTestsOfKind(TestKind.Integration));

        Target TestEndToEnd => d => d
            .Description("Runs test projects: EndToEnd")
            .DependsOn(Compile, SetTestEnvironmentVariables)
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
            .Executes(() => DotNet("format " + LintFlags(), RootDirectory));

        /// <summary>
        /// Replaces <see cref="IDefaultBuild.Test"/>, which every release target depends on, so that releasing only
        /// runs unit tests: integration and end-to-end tests need infrastructure the build agent may not have.
        /// </summary>
        Target IDefaultBuild.Test => d => d
            .Description("Tests ran during the default build and releases (unit only)")
            .DependsOn(TestUnit);

        Target IDefaultBuild.PullRequest => d => d
            .Description("Runs all tests and validations for the Pull Request CI-pipeline")
            .DependsOn(Lint, TestAll);

        string LintFlags()
        {
            var result = new List<string> { "--severity", "warn" };
            if (IsRestored) result.Add("--no-restore");
            if (IsCiBuild) result.Add("--verbosity diagnostic");

            return result.Join(" ");
        }
    }
}
