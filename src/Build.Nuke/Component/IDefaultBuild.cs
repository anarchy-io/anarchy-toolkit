using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Entity.UserSecrets;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Extension;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities.Collections;
using Environment = Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build.Environment;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    // `Configuration` would otherwise resolve to this project's Configuration namespace (see the architecture guide).
    using Configuration = Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build.Configuration;

    public interface IDefaultBuild : INukeBuild
    {
        [Parameter("Configuration to build - (Debug or Release; defaults to Release)")]
        Configuration Configuration => TryGetValue(() => Configuration)
                                    ?? Configuration.Release;

        Environment? BuildEnvironment => WindowsBuildEnvironment ?? LinuxBuildEnvironment;

        [Parameter("Environment the build is running in (typically configured by environment variable)", Name = "ASPNETCORE_ENVIRONMENT")]
        Environment? WindowsBuildEnvironment => TryGetValue(() => WindowsBuildEnvironment);

        [Parameter("Environment the build is running in (typically configured by environment variable)", Name = "DOTNET_ENVIRONMENT")]
        Environment? LinuxBuildEnvironment => TryGetValue(() => LinuxBuildEnvironment);

        // these two are only defined here so they show up in documentation -- Kms defines its own parameters
        [Parameter("AWS credentials profile name")]
        public string? AwsProfile => TryGetValue(() => AwsProfile);

        [Parameter("AWS default region")]
        public string? AwsRegion => TryGetValue(() => AwsRegion);

        [Parameter("Artifacts JSON output file")]
        public string? ArtifactsJsonOutput => null;

        [Solution]
        Solution Solution => TryGetValue(() => Solution);

        [GitRepository]
        GitRepository? GitRepository => TryGetValue(() => GitRepository);

        /// <summary>
        /// Root directory for all 'production code' projects. By default refers to 'src'.
        /// </summary>
        AbsolutePath SourceDirectory => RootDirectory / "src";

        /// <summary>
        /// Root directory for all test projects. By default refers to 'test'.
        /// </summary>
        AbsolutePath TestsDirectory => RootDirectory / "test";

        /// <summary>
        /// Directory where build artifacts are to be stored. By default refers to 'artifacts'.
        /// </summary>
        AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

        /// <summary>
        /// All projects that should be published and deployed. The <see cref="PullRequest"/>,
        /// and <see cref="ICodeDeployBuild.BuildReleaseCodeDeploy"/> targets build
        /// or release everything defined in this collection. <see cref="ICodeDeployBuild.WriteBuildMetadata"/> writes metadata for CodeDeploy
        /// artifacts defined in this collection.
        /// </summary>
        IReadOnlyCollection<Artifact> Artifacts { get; }

        /// <summary>
        /// Environment variables to be set when executing tests on TeamCity. Keys are used verbatim. If the value
        /// is a <see cref="ConfigurationValue"/> (or a string), then the value is also used as-is. If the value is a
        /// <see cref="Secret"/>, then it is decrypted using KMS.
        /// </summary>
        IReadOnlyDictionary<string, ConfigurationValue> TestConfiguration => new Dictionary<string, ConfigurationValue>();

        /// <summary>
        /// Whether or not to treat warnings as error for <see cref="Build"/>, <see cref="ICodeDeployBuild.PublishLinux"/>.
        /// Defaults to true. It is highly recommended to keep it that way and fix your
        /// warnings instead of allowing warnings to exist.
        /// </summary>
        bool TreatWarningsAsErrors => true;

        /// <summary>
        /// <para>
        /// The test filter to apply when executing tests in <see cref="Test"/> that run through
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
        /// For more information: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test#filter-option-details
        /// </para>
        /// </summary>
        string? TestFilter => null;

        /// <summary>
        /// <para>
        /// The options to pass to the test projects in <see cref="Test"/> that run through
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
        /// <para>
        /// A list of all projects considered test projects. By default this returns any project in the solution for
        /// which <see cref="IDefaultBuild.IsTestProject"/> returns <c>true</c> and <see cref="IHasAcceptanceTests.IsAcceptanceTestProject"/> returns
        /// <c>false</c>.
        /// </para>
        ///
        /// <para>
        /// <see cref="Test"/> executes tests in all of these projects.
        /// </para>
        /// </summary>
        IReadOnlyCollection<AbsolutePath> TestProjects =>
            // ReSharper disable once SuspiciousTypeConversion.Global
            Solution.AllProjects
                    .Where(
                         p => IsTestProject(p)
                           && (!(this is IHasAcceptanceTests hasAcceptanceTests)
                            || !hasAcceptanceTests.IsAcceptanceTestProject(p))
                     )
                    .Select(p => p.Path)
                    .ToList();

        /// <summary>
        /// <para>
        /// User secrets configuration for local development.
        /// </para>
        ///
        /// <para>
        /// Use <see cref="DotNetUserSecretsProject.ProjectFile"/> to specify for which project set user secrets <see cref="DotNetUserSecretsProject.UserSecrets"/> for.
        /// </para>
        ///
        /// <code>
        /// public IReadOnlyCollection&lt;DotNetUserSecretsProject&gt; UserSecretsProjects =>
        ///     new List&lt;DotNetUserSecretsProject&gt;
        ///     {
        ///         new DotNetUserSecretsProject
        ///         {
        ///             ProjectFile = this.As&lt;IDefaultBuild&gt;().SourceDirectory / "InternalOrders.Api" / "InternalOrders.Api.csproj",
        ///             UserSecrets =
        ///             {
        ///                 { "OracleSettings:OraclePassword", OraclePasswordPromptedValue },
        ///                 { "OracleSettings:OracleHost", "oracle-vanessa-main" },
        ///                 { "ExternalApiUri", ExternalApiPromptedValue }
        ///             }
        ///         }
        ///     };
        /// 
        /// PromptedSensitiveDotNetUserSecretValue OraclePasswordPromptedValue = new PromptedSensitiveDotNetUserSecretValue("Oracle password");
        /// PromptedDotNetUserSecretValue ExternalApiPromptedValue = new PromptedDotNetUserSecretValue("External API uri");
        /// </code>
        /// </summary>
        IReadOnlyCollection<DotNetUserSecretsProject> UserSecretsProjects => new List<DotNetUserSecretsProject>();

        /// <summary>
        /// Performs all the steps necessary to validate a pull request. This executes <see cref="Build"/> and
        /// <see cref="Test"/>.
        /// </summary>
        Target PullRequest =>
            _ => _
               .DependsOn(Build, Test);

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
        /// Runs <c>dotnet build</c> on the entire solution, to verify there are no compiler errors that are not found
        /// by <see cref="ICodeDeployBuild.PublishLinux"/>. Strictly speaking, this is a safety measure,
        /// but to decrease build time, it is depended on by <see cref="Test"/>.
        /// </summary>
        Target Build =>
            _ => _
                .DependsOn(Clean)
                .Unlisted()
                .Executes(this.BuildSolution);

        /// <summary>
        /// Runs integration and unit tests. It runs tests in all projects defined in <see cref="IDefaultBuild.TestProjects"/>. To
        /// include or exclude tests, categorize them (for example by applying <c>[Trait("Category", "NoCI")]></c>),
        /// then override the <see cref="IDefaultBuild.TestFilter"/> property (to return, for example <c>Category!=NoCI</c>).
        /// </summary>
        Target Test =>
            _ => _
                .DependsOn(Build, SetTestEnvironmentVariables)
                .Unlisted()
                .Executes(this.RunTests);

        /// <summary>
        /// Sets up environment variables needed to run tests (the <see cref="Test"/> target) on TeamCity.
        /// </summary>
        Target SetTestEnvironmentVariables =>
            _ => _
                .OnlyWhenDynamic(() => IsServerBuild)
                .Unlisted()
                .Executes(() => this.SetEnvironmentVariables(TestConfiguration));

        /// <summary>
        /// Sets user secrets specified in <see cref="UserSecretsProjects"/>.
        /// </summary>
        Target SetUserSecrets =>
            _ => _.Executes(() => UserSecretsTasks.SetUserSecrets(this))
                  .Unlisted()
                  .TriggeredBy(SetupLocal);

        /// <summary>
        /// A target to setup local settings. By default triggers <see cref="SetUserSecrets"/>.
        /// </summary>
        Target SetupLocal =>
            _ => _;

        /// <summary>
        /// Determines whether the project specified should be considered a test project (executed during
        /// <see cref="Test"/>). By default, returns <c>true</c> if the project contains a reference to either
        /// <c>Microsoft.NET.Test.Sdk</c> or <c>xunit.runner.visualstudio</c>.
        /// </summary>
        bool IsTestProject(Project project) =>
            project.ContainsPackageReferenceToAny(TestTasks.DefaultTestPackages);

        Target PrintArtifactsJson =>
            _ => _.Executes(() => ArtifactsTasks.PrintArtifactsJson(this, ArtifactsJsonOutput));
    }
}
