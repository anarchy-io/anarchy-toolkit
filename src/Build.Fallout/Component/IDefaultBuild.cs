using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities.Collections;
using Fallout.Solutions;
using Environment = Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build.Environment;

namespace Anarchy.Toolkit.Build.Fallout.Component;

// `Configuration` would otherwise resolve to this project's Configuration namespace (see the architecture guide).
using Configuration = Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build.Configuration;

/// <summary>
/// The base build: configuration, environment, solution and directory conventions, plus the unlisted
/// <see cref="Clean"/> and <see cref="Build"/> targets and the test options shared by every test runner.
/// </summary>
public interface IDefaultBuild : IFalloutBuild
{
    [Parameter("Configuration to build - (Debug or Release; defaults to Release)")]
    Configuration Configuration => TryGetValue(() => Configuration)
                                   ?? Configuration.Release;

    [Parameter("Indicates whether the build runs on CI")]
    bool IsCiBuild => TryGetValue<bool?>(() => IsCiBuild) ?? false;

    /// <summary>
    /// The verbosity of every <c>dotnet</c> command the targets run: <see cref="DotNetVerbosity.minimal"/>, or
    /// <see cref="DotNetVerbosity.diagnostic"/> on CI (<see cref="IsCiBuild"/>), where the extra detail is what
    /// helps investigate a failed run.
    /// </summary>
    DotNetVerbosity ToolVerbosity => IsCiBuild ? DotNetVerbosity.diagnostic : DotNetVerbosity.minimal;

    /// <summary>
    /// The environment the build runs in: <see cref="WindowsBuildEnvironment"/> (<c>ASPNETCORE_ENVIRONMENT</c>)
    /// when set, otherwise <see cref="LinuxBuildEnvironment"/> (<c>DOTNET_ENVIRONMENT</c>).
    /// </summary>
    Environment? BuildEnvironment => WindowsBuildEnvironment ?? LinuxBuildEnvironment;

    [Parameter("Environment the build is running in (typically configured by environment variable)", Name = "ASPNETCORE_ENVIRONMENT")]
    Environment? WindowsBuildEnvironment => TryGetValue(() => WindowsBuildEnvironment);

    [Parameter("Environment the build is running in (typically configured by environment variable)", Name = "DOTNET_ENVIRONMENT")]
    Environment? LinuxBuildEnvironment => TryGetValue(() => LinuxBuildEnvironment);

    [Solution]
    Solution Solution => TryGetValue(() => Solution);

    [GitRepository]
    GitRepository? GitRepository => TryGetValue(() => GitRepository);

    /// <summary>
    /// Root directory for all production code projects. Refers to <c>src</c> by default.
    /// </summary>
    AbsolutePath SourceDirectory => RootDirectory / "src";

    /// <summary>
    /// Root directory for all test projects. Refers to <c>test</c> by default; <see cref="IStandardBuild"/>
    /// overrides it to <c>tests</c>.
    /// </summary>
    AbsolutePath TestsDirectory => RootDirectory / "test";

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
    bool TreatWarningsAsErrors => true;

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
    string? TestFilter => null;

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
    string? TestingPlatformCommandLineArguments => null;

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
    bool IsTestProject(Project project) =>
        project.ContainsPackageReferenceToAny(TestTasks.DefaultTestPackages);
}
