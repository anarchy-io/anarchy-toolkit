using Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks;

/// <summary>
/// Tasks related to executing tests.
/// </summary>
public static class TestTasks
{
    /// <summary>
    /// Default packages that mark a project as a test project.
    /// </summary>
    public static readonly string[] DefaultTestPackages =
    {
        "Microsoft.NET.Test.Sdk",
        "xunit.runner.visualstudio",
        "NUnit3TestAdapter",
        // xUnit.net v3 projects need neither of the VSTest packages above, so without this a project that only
        // references xunit.v3 would not be recognized as a test project at all and its tests would silently
        // never run.
        "xunit.v3",
    };

    /// <summary>
    /// Runs the tests of the test projects specified, with the runner each one needs: projects built on
    /// Microsoft.Testing.Platform run through it directly (see <see cref="TestingPlatformHelper"/>), all others
    /// through <c>dotnet test</c>.
    /// </summary>
    /// <param name="build">The build providing the configuration and the test options.</param>
    /// <param name="projectFiles">The test projects to run.</param>
    /// <param name="noBuild">
    /// Whether the projects have already been restored and built (for example by a <c>Compile</c> target).
    /// </param>
    public static void RunTests(this IDefaultBuild build, IEnumerable<AbsolutePath> projectFiles, bool noBuild)
    {
        var (testingPlatformProjects, vsTestProjects) =
            TestingPlatformHelper.PartitionByRunner(build.Solution, projectFiles);

        AssertTestFilterIsSupported(build, testingPlatformProjects);

        foreach (var projectFile in testingPlatformProjects)
        {
            TestingPlatformHelper.RunTests(
                projectFile,
                build.Configuration,
                build.TestingPlatformCommandLineArguments,
                noBuild,
                build.ToolVerbosity
            );
        }

        DotNetTasks.DotNetTest(
            s => s
                .SetVerbosity(build.ToolVerbosity)
                .SetConfiguration(build.Configuration)
                .SetFilter(build.TestFilter)
                .SetNoBuild(noBuild)
                .CombineWith(
                    c => vsTestProjects.Select(p => c.SetProjectFile(p))
                )
        );
    }

    /// <summary>
    /// <para>
    /// Fails the build when <see cref="IDefaultBuild.TestFilter"/> is set while test projects run through
    /// Microsoft.Testing.Platform and <see cref="IDefaultBuild.TestingPlatformCommandLineArguments"/> is not.
    /// </para>
    ///
    /// <para>
    /// <see cref="IDefaultBuild.TestFilter"/> is a <c>dotnet test</c> filter expression, which the platform
    /// does not accept — xUnit.net v3 3.x has no <c>--filter</c> option at all. Failing here, naming the
    /// property to set, beats the platform answering an option it does not know with its full list of options.
    /// </para>
    /// </summary>
    private static void AssertTestFilterIsSupported(
        IDefaultBuild build,
        IReadOnlyCollection<AbsolutePath> testingPlatformProjects
    )
    {
        if (testingPlatformProjects.Count == 0
            || string.IsNullOrWhiteSpace(build.TestFilter)
            || !string.IsNullOrWhiteSpace(build.TestingPlatformCommandLineArguments))
        {
            return;
        }

        Assert.Fail(
            $"""
             TestFilter is set to ‘{build.TestFilter}’, but {testingPlatformProjects.Count} test project(s) run through Microsoft.Testing.Platform, which does not accept a `dotnet test` filter expression.
             Override TestingPlatformCommandLineArguments with the equivalent platform options, for example ‘--filter-not-trait Category=NoCI’. See the Test runners section of the Anarchy.Toolkit.Build.Fallout guide.
             Affected projects: {string.Join(", ", testingPlatformProjects.Select(p => p.Name))}
             """
        );
    }
}
