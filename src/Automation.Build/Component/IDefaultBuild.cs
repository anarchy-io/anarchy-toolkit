using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Build.Core.Entity.Build;
using Anarchy.Toolkit.Automation.Build.Core.ValueSet.Build;
using Anarchy.Toolkit.Automation.Build.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities.Collections;
using Fallout.Solutions;
namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// The base build: configuration, environment, solution and directory conventions, plus the unlisted
/// <see cref="Clean"/> and <see cref="Build"/> targets and the test options shared by every test runner.
/// </summary>
/// <remarks>
/// <para>
/// Anarchy build components are interfaces with default members: a build class implements the ones it needs and
/// overrides members (explicitly, e.g. <c>AbsolutePath IDefaultBuild.SourceDirectory =&gt; ...</c>) to change
/// conventions. Most builds implement <see cref="IStandardBuild"/>, which extends this one.
/// </para>
/// <para>
/// <see cref="Artifacts"/> is the only member without a default and must be implemented.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// partial class Build : AnarchyBuild, IDefaultBuild
/// {
///     public static int Main() => Execute&lt;Build&gt;(x => x.As&lt;IDefaultBuild&gt;().Build);
///
///     public IReadOnlyCollection&lt;Artifact&gt; Artifacts => [];
///
///     string? IDefaultBuild.TestingPlatformCommandLineArguments => "--filter-not-trait Category=NoCI";
/// }
/// </code>
/// </example>
public interface IDefaultBuild : IAutomationPreferencesBuild
{
    /// <summary>
    /// The configuration to build (<c>--configuration</c>): <see cref="ConfigurationValueSet.Debug"/> or
    /// <see cref="ConfigurationValueSet.Release"/>, the default.
    /// </summary>
    [Parameter("Configuration to build - (Debug or Release; defaults to Release)")]
    ConfigurationValueSet Configuration => TryGetValue(() => Configuration)
                                            ?? Preference<ConfigurationValueSet>("build.configuration")
                                            ?? ConfigurationValueSet.Release;

    /// <summary>
    /// Whether the build runs on CI (<c>--is-ci-build</c>). Defaults to <see langword="false"/>. Raises
    /// <see cref="ToolVerbosity"/> and disables interactive prompts.
    /// </summary>
    [Parameter("Indicates whether the build runs on CI")]
    bool IsCiBuild => TryGetValue<bool?>(() => IsCiBuild) ?? false;

    /// <summary>
    /// The verbosity of every <c>dotnet</c> command the targets run: <see cref="DotNetVerbosity.minimal"/>, or
    /// <see cref="DotNetVerbosity.diagnostic"/> on CI (<see cref="IsCiBuild"/>), where the extra detail is what
    /// helps investigate a failed run.
    /// An explicitly quiet Anarchy build also defaults tool output to quiet; informational target tables honor it.
    /// </summary>
    [Parameter("Verbosity of dotnet commands (default: diagnostic on CI, minimal otherwise)")]
    DotNetVerbosity ToolVerbosity => TryGetValue(() => ToolVerbosity)
                                    ?? (Verbosity == Fallout.Common.Verbosity.Quiet ? DotNetVerbosity.quiet
                                        : Preference<string>("build.toolVerbosity") is { } value
                                        ? (DotNetVerbosity)value
                                        : IsCiBuild ? DotNetVerbosity.diagnostic : DotNetVerbosity.minimal);

    /// <summary>
    /// The environment the build runs in: <see cref="WindowsBuildEnvironment"/> (<c>ASPNETCORE_ENVIRONMENT</c>)
    /// when set, otherwise <see cref="LinuxBuildEnvironment"/> (<c>DOTNET_ENVIRONMENT</c>).
    /// </summary>
    EnvironmentValueSet? BuildEnvironment => WindowsBuildEnvironment ?? LinuxBuildEnvironment;

    /// <summary>
    /// The environment from the <c>ASPNETCORE_ENVIRONMENT</c> parameter or environment variable, if any.
    /// </summary>
    [Parameter("Environment the build is running in (typically configured by environment variable)", Name = "ASPNETCORE_ENVIRONMENT")]
    EnvironmentValueSet? WindowsBuildEnvironment => TryGetValue(() => WindowsBuildEnvironment);

    /// <summary>
    /// The environment from the <c>DOTNET_ENVIRONMENT</c> parameter or environment variable, if any.
    /// </summary>
    [Parameter("Environment the build is running in (typically configured by environment variable)", Name = "DOTNET_ENVIRONMENT")]
    EnvironmentValueSet? LinuxBuildEnvironment => TryGetValue(() => LinuxBuildEnvironment);

    /// <summary>
    /// The solution the build works on, as injected by Fallout's <c>[Solution]</c> attribute (typically configured in
    /// <c>.fallout/parameters.json</c>).
    /// </summary>
    [Solution]
    Solution Solution => TryGetValue(() => Solution);

    /// <summary>
    /// The Git repository the build runs in, or <see langword="null"/> outside one.
    /// </summary>
    [GitRepository]
    GitRepository? GitRepository => TryGetValue(() => GitRepository);

    /// <summary>
    /// The production code folder relative to the root (<c>--source-root</c>, or <c>SourceRoot</c> in
    /// <c>.fallout/parameters.json</c>); <see langword="null"/> keeps <c>src</c>. See <see cref="SourceDirectory"/>.
    /// </summary>
    [Parameter("Production code folder, relative to the root (default: src)")]
    string? SourceRoot => TryGetValue(() => SourceRoot) ?? Preference<string>("build.sourceRoot");

    /// <summary>
    /// The test folder relative to the root (<c>--tests-root</c>, or <c>TestsRoot</c> in
    /// <c>.fallout/parameters.json</c>); <see langword="null"/> keeps the default of <see cref="TestsDirectory"/>.
    /// </summary>
    [Parameter("Test projects folder, relative to the root (default: tests with IStandardBuild, else test)")]
    string? TestsRoot => TryGetValue(() => TestsRoot) ?? Preference<string>("build.testsRoot");

    /// <summary>
    /// Root directory for all production code projects: <see cref="SourceRoot"/>, else <c>src</c>. A build can also
    /// override it (<c>AbsolutePath IDefaultBuild.SourceDirectory =&gt; RootDirectory / "code";</c>).
    /// </summary>
    AbsolutePath SourceDirectory => RootDirectory / (SourceRoot ?? "src");

    /// <summary>
    /// Root directory for all test projects: <see cref="TestsRoot"/>, else <c>test</c>; <see cref="IStandardBuild"/>
    /// defaults it to <c>tests</c>. A build can also override it.
    /// </summary>
    AbsolutePath TestsDirectory => RootDirectory / (TestsRoot ?? "test");

    /// <summary>
    /// Directory where build artifacts are to be stored. Refers to <c>artifacts</c> by default.
    /// </summary>
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    /// <summary>
    /// All projects that should be published and deployed.
    /// </summary>
    IReadOnlyCollection<Artifact> Artifacts { get; }

    /// <summary>
    /// Whether to treat warnings as errors when compiling the solution (<see cref="Build"/>).
    /// Defaults to <c>true</c>. It is highly recommended to keep it that way and fix your
    /// warnings instead of allowing warnings to exist.
    /// </summary>
    [Parameter("Treat build warnings as errors (default: true)")]
    bool TreatWarningsAsErrors => TryGetValue<bool?>(() => TreatWarningsAsErrors) ?? Preference<bool?>("build.treatWarningsAsErrors") ?? true;

    /// <summary>
    /// <para>
    /// The test filter to apply when executing tests that run through
    /// <c>dotnet test</c>. This is expressed as the <c>--filter</c> parameter when invoking it.
    /// </para>
    ///
    /// <para>
    /// Test projects built on Microsoft.Testing.Platform do not run through <c>dotnet test</c> and do not
    /// accept this expression; they take their filter, and any other option, from
    /// <see cref="TestingPlatformCommandLineArguments"/> instead. Setting this while such a project is
    /// present, without setting that one, fails the build.
    /// </para>
    ///
    /// <para>
    /// For more information: <see href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test#filter-option-details"/>.
    /// </para>
    /// </summary>
    [Parameter("VSTest filter (not MTP syntax)")]
    string? TestFilter => TryGetValue(() => TestFilter) ?? Preference<string>("tests.filter");

    /// <summary>
    /// <para>
    /// The options to pass to the test projects that run through
    /// Microsoft.Testing.Platform, which is every project referencing <c>xunit.v3</c>. This is expressed as
    /// that platform's own command line and passed through untouched, for example
    /// <c>--filter-not-trait Category=NoCI</c>. Quoted values are supported:
    /// <c>--filter-not-trait Category="Slow Tests"</c>.
    /// </para>
    ///
    /// <para>
    /// The name is that of the MSBuild property this ends up in, and the value is added to whatever a project
    /// sets in that property itself rather than replacing it. Options that do not change from one build to
    /// the next — collecting coverage, for instance — are therefore better set in the project, where a plain
    /// <c>dotnet test</c> run picks them up as well; reserve this for what the build has to work out for
    /// itself.
    /// </para>
    ///
    /// <para>
    /// Filtering is the usual reason to set it, and is why this exists next to <see cref="TestFilter"/>
    /// instead of sharing it: the two runners take different filter syntax, and one solution can hold
    /// projects of both kinds. Note that only xUnit.net v3 4.0 and later offer a <c>--filter</c> option
    /// taking a <c>dotnet test</c> style expression, while <c>--filter-trait</c> and
    /// <c>--filter-not-trait</c> work on every version.
    /// </para>
    /// </summary>
    [Parameter("Microsoft.Testing.Platform arguments")]
    string? TestingPlatformCommandLineArguments => TryGetValue(() => TestingPlatformCommandLineArguments) ?? Preference<string>("tests.testingPlatformArguments");

    /// <summary>
    /// Cleans solution of build artifacts. It deletes all <c>bin</c> and <c>obj</c> directories in
    /// <see cref="SourceDirectory"/> and <see cref="TestsDirectory"/> and ensures <see cref="ArtifactsDirectory"/>
    /// exists but is empty.
    /// </summary>
    Target Clean =>
        _ => _
            .Unlisted()
            .Executes(
                () => {
                    SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(dir => dir.DeleteDirectory());
                    TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(dir => dir.DeleteDirectory());
                    ArtifactsDirectory.CreateOrCleanDirectory();
                }
            );

    /// <summary>
    /// Runs <c>dotnet build</c> on the entire solution, at <see cref="ToolVerbosity"/>, after <see cref="Clean"/>.
    /// </summary>
    Target Build =>
        _ => _
            .DependsOn(Clean)
            .Unlisted()
            .Executes(this.BuildSolution);

    /// <summary>
    /// Determines whether the project specified should be considered a test project. By default, returns
    /// <c>true</c> if the project references any of <see cref="TestTasks.DefaultTestPackages"/>
    /// (<c>Microsoft.NET.Test.Sdk</c>, <c>xunit.runner.visualstudio</c>, <c>NUnit3TestAdapter</c> or <c>xunit.v3</c>).
    /// </summary>
    /// <param name="project">The solution project to consider.</param>
    /// <returns><see langword="true"/> if the project holds tests; otherwise <see langword="false"/>.</returns>
    bool IsTestProject(Project project) =>
        project.ContainsPackageReferenceToAny(TestTasks.DefaultTestPackages);
}
