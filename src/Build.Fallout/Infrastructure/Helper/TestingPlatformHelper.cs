using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Solutions;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper;

/// <summary>
/// <para>
/// Helpers to run test projects built on Microsoft.Testing.Platform (MTP), which is what xUnit.net v3 uses.
/// </para>
///
/// <para>
/// MTP 2 (used by <c>xunit.v3</c> 4.0 and later) refuses to run through the VSTest target that
/// <c>dotnet test</c> invokes by default on the .NET 10 SDK and later, failing the build with ‘Testing with
/// VSTest target is no longer supported by Microsoft.Testing.Platform’. Opting in to the MTP-based
/// <c>dotnet test</c> instead would require a <c>test</c> section in the <c>global.json</c> of every consumer,
/// so these projects are run through the MSBuild targets that MTP injects into them: those need no opt-in and
/// work just as well for MTP 1 (<c>xunit.v3</c> 3.x).
/// </para>
/// </summary>
public static class TestingPlatformHelper
{
    /// <summary>
    /// Packages that always bring in Microsoft.Testing.Platform, and so mark a project as one that has to be
    /// run through <see cref="RunTests"/> instead of <c>dotnet test</c>.
    /// </summary>
    public static readonly string[] DefaultTestingPlatformPackages = {
        "xunit.v3",
    };

    /// <summary>
    /// <para>
    /// The MSBuild target that builds a test project and then runs it through Microsoft.Testing.Platform.
    /// </para>
    ///
    /// <para>
    /// The platform injects a second target, <c>InvokeTestingPlatform</c>, that runs the tests without
    /// building first — the equivalent of <c>dotnet test --no-build</c>. It is deliberately not used. That
    /// target reads <c>$(TargetPath)</c>, which is not set in the outer build of a project that uses the
    /// plural <c>TargetFrameworks</c> property, even when that names a single framework; run against one it
    /// fails with ‘the "InvokeTestingPlatformTask" task was not given a value for the required parameter
    /// "TargetPath"’. Only <c>Test</c> dispatches to each target framework. What it costs is negligible:
    /// on the no-build path the build it runs is up to date by then anyway.
    /// </para>
    /// </summary>
    private const string TestTarget = "Test";

    /// <summary>
    /// The MSBuild property through which options reach Microsoft.Testing.Platform. A project can set it
    /// itself, which is why <see cref="RunTests"/> adds to it instead of overwriting it.
    /// </summary>
    private const string ArgumentsProperty = "TestingPlatformCommandLineArguments";

    /// <summary>
    /// <para>
    /// Returns whether the test project specified is built on Microsoft.Testing.Platform.
    /// </para>
    ///
    /// <para>
    /// A reference to any of <see cref="DefaultTestingPlatformPackages"/> is enough to tell. For anything else
    /// (or for a project that is not part of the solution) the <c>IsTestingPlatformApplication</c> property is
    /// read from the project, which is the very property Microsoft.Testing.Platform itself keys off. That
    /// property is only set once a project has been restored, hence the package check first: it holds for
    /// projects that have not been restored yet as well.
    /// </para>
    /// </summary>
    public static bool IsTestingPlatformProject(Solution? solution, AbsolutePath projectFile)
    {
        var project = solution?.AllProjects.FirstOrDefault(p => p.Path == projectFile);

        return project != null && project.ContainsPackageReferenceToAny(DefaultTestingPlatformPackages)
               || IsTestingPlatformApplication(projectFile);
    }

    /// <summary>
    /// Splits the test projects specified into the ones that have to run through Microsoft.Testing.Platform
    /// (see <see cref="RunTests"/>) and the ones that run through <c>dotnet test</c>, as determined by
    /// <see cref="IsTestingPlatformProject"/>.
    /// </summary>
    public static (IReadOnlyCollection<AbsolutePath> TestingPlatform, IReadOnlyCollection<AbsolutePath> VSTest)
        PartitionByRunner(Solution? solution, IEnumerable<AbsolutePath> projectFiles)
    {
        var projectsByRunner = projectFiles.ToLookup(p => IsTestingPlatformProject(solution, p));

        return (projectsByRunner[true]
            .ToList(), projectsByRunner[false]
            .ToList());
    }

    /// <summary>
    /// Runs the test project specified through Microsoft.Testing.Platform.
    /// </summary>
    /// <param name="projectFile">The test project to run.</param>
    /// <param name="configuration">The configuration to run the tests in.</param>
    /// <param name="commandLineArguments">
    /// Optional options for the platform, expressed as its own command line (for example
    /// <c>--filter-not-trait Category=NoCI</c>) and passed through untouched, after any the project sets
    /// itself. This is not a <c>dotnet test</c> filter expression; see
    /// <c>IDefaultBuild.TestingPlatformCommandLineArguments</c>.
    /// </param>
    /// <param name="noBuild">
    /// Whether the project has already been built, and so needs neither restoring nor building again. The
    /// build still runs as part of <see cref="TestTarget"/>, but finds everything up to date.
    /// </param>
    /// <param name="verbosity">
    /// The MSBuild verbosity (default: <see cref="DotNetVerbosity.minimal"/>). MSBuild only relays what the test
    /// application writes, so a higher one mostly buries the test results.
    /// </param>
    public static void RunTests(
        AbsolutePath projectFile,
        string configuration,
        string? commandLineArguments = null,
        bool noBuild = false,
        DotNetVerbosity? verbosity = null
    ) => DotNetTasks.DotNetMSBuild(s => {
        var settings = s
            .SetVerbosity(verbosity ?? DotNetVerbosity.minimal)
            .SetTargetPath(projectFile)
            .SetTargets(TestTarget)
            .SetConfiguration(configuration)
            // Unlike `dotnet test`, MSBuild does not restore implicitly.
            .SetRestore(!noBuild)
            // The Test target always runs Build (so NoBuild cannot be used), but when the solution is
            // already built, the referenced projects need not be visited again, each relaying its output path.
            .SetProperty("BuildProjectReferences", (!noBuild).ToString().ToLowerInvariant())
            // Without this, Microsoft.Testing.Platform swallows everything the test
            // application writes, including the CI service messages that xUnit.net
            // emits when it detects it runs on CI.
            .SetProperty("TestingPlatformCaptureOutput", new[] {
                    DotNetVerbosity.quiet,
                    DotNetVerbosity.minimal
            }.Contains(verbosity) ?
                "false" :
                "true")
            // Report failing tests and their assertion output as build errors, instead of
            // only pointing at a log file on disk.
            .SetProperty("TestingPlatformShowTestsFailure", new[] {
                    DotNetVerbosity.quiet,
                    DotNetVerbosity.minimal
            }.Contains(verbosity) ?
                "false" :
                "true");

        if (string.IsNullOrWhiteSpace(commandLineArguments))
        {
            return settings;
        }

        // The project's own value is read and passed back on, because a property given on the
        // command line overrides the assignment in the project — including the
        // `$(TestingPlatformCommandLineArguments)` self-reference it usually appends to. Without
        // this, a project that collects coverage would silently stop doing so under a filter.
        //
        // MSBuild strips plain quotes when parsing `/property:`, while the test platform splits this
        // property on whitespace. %22 is MSBuild's escape for a quote and survives both, so that a
        // filter value such as "Slow Tests" reaches the platform in one piece.
        return settings.SetProperty(
            ArgumentsProperty,
            $"{ReadProjectCommandLineArguments(projectFile, configuration)} {commandLineArguments}"
                .Trim()
                .Replace("\"", "%22")
        );
    }
    );

    /// <summary>
    /// Reads the <c>TestingPlatformCommandLineArguments</c> the project specified sets itself, so that a
    /// build’s own can be added to them rather than replace them. Returns <c>null</c> when it sets none.
    /// </summary>
    private static string? ReadProjectCommandLineArguments(AbsolutePath projectFile, string configuration) =>
        DotNetTasks
            .DotNet(
                $"msbuild \"{projectFile}\" -getProperty:{ArgumentsProperty} -p:Configuration={configuration}",
                logOutput: false,
                logInvocation: false,
                exitHandler: _ => null
            )
            .Select(o => o.Text.Trim())
            .FirstOrDefault(o => !string.IsNullOrEmpty(o));

    /// <summary>
    /// Reads the <c>IsTestingPlatformApplication</c> property from the project specified. Returns <c>false</c>
    /// when the property cannot be read, for instance because the project has not been restored yet.
    /// </summary>
    private static bool IsTestingPlatformApplication(AbsolutePath projectFile) =>
        DotNetTasks
            .DotNet(
                // The project path is quoted because MSBuild is given one argument string: unquoted, a path
                // containing a space is split into two projects and MSBuild answers with MSB1008, which the
                // exit handler below would then hide.
                $"msbuild \"{projectFile}\" -getProperty:IsTestingPlatformApplication",
                logOutput: false,
                logInvocation: false,
                exitHandler: _ => null
            )
            .Any(o => o.Text.Trim()
                .Equals("true", StringComparison.OrdinalIgnoreCase));
}
