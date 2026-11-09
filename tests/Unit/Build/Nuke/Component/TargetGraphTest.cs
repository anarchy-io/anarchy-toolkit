using Anarchy.Toolkit.Build.Nuke.Component;
using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using AwesomeAssertions;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using BuildEnvironment = Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build.Environment;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class TargetGraphTest
{
    // Exercise NUKE's actual component resolution without running any publishing or deployment tools.
    // Each row is target | direct dependencies | action count | dynamic conditions | requirements.
    private const string TargetSpecifications = """
        AnnounceCodeDeployArtifactsToTeamCity||1|1|0
        AwsCliPackage|PublishLambda|1|1|0
        Build|Clean|1|0|0
        BuildRelease|Clean,Build,Test,DeployServer|0|0|0
        BuildReleaseCodeDeploy|Build,Test,PackCodeDeploy|0|0|0
        BuildReleaseFull|PublishLambda,BuildReleaseCodeDeploy|0|0|0
        CheckLayering||1|0|0
        Clean||1|0|0
        Compile|Restore|1|0|0
        ConfigureLocalCloudformationParameters||1|0|0
        DeployArtifacts||1|0|0
        DeployLocal|PublishLambda|1|0|0
        DeployServer|PublishLambda,AwsCliPackage,PublishLambdaCI|0|1|0
        DisableGitHook||1|0|0
        EcrImageBuild|EcrPublishAppArtifact|1|0|0
        EcrImagePush|EcrImageBuild|1|0|0
        EcrPublishAppArtifact|Compile|1|0|2
        EnableGitHook||1|0|0
        EndToEndTest|SetEndToEndTestEnvironmentVariables|1|0|0
        GenerateAppSettings||1|0|0
        GenerateDependencyMap||1|0|0
        GenerateLocalRelease||1|0|2
        GenerateReleaseFromChanges|Restore|1|0|1
        GenerateReleaseNotes|Restore|1|0|1
        Lint|Restore|1|0|0
        LintFix|Restore|1|0|0
        ListPackages||1|0|0
        ListProjects||1|0|0
        Pack|Compile|1|0|0
        PackCodeDeploy|PublishLinux,ValidateCodeDeploySettings|1|0|0
        PrintArtifactsJson||1|0|0
        Publish|Pack|1|0|1
        PublishLambda|Clean|1|1|0
        PublishLambdaCI|Clean|1|1|0
        PublishLinux|Clean,WriteBuildMetadata|1|0|0
        PullRequest|Lint,TestAll|0|0|0
        ReportFailure||1|1|0
        Restore||1|0|0
        Run|Clean,Build,Test,PublishLambda,DeployLocal|0|0|0
        RunApp||1|0|0
        RunApps||1|0|0
        SetEndToEndTestEnvironmentVariables||1|1|0
        SetTestEnvironmentVariables||1|1|0
        SetupBoilerplate||1|0|0
        SetupLocal||0|0|0
        SetUserSecrets||1|0|0
        ShowGitHooks||1|0|0
        Test|TestUnit|0|0|0
        TestAll|TestUnit,TestIntegration,TestEndToEnd|0|0|0
        TestEndToEnd|Compile,SetTestEnvironmentVariables|1|0|0
        TestIntegration|Compile,SetTestEnvironmentVariables|1|0|0
        TestUnit|Compile,SetTestEnvironmentVariables|1|0|0
        UpgradeDependencies||1|0|0
        ValidateCodeDeploySettings||1|0|0
        ValidateDeploymentAcceptance||0|0|0
        ValidateLocalRelease||1|0|1
        WriteBuildMetadata||1|0|0
        """;

    public static IEnumerable<object[]> TargetCases => TargetSpecifications.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim().Split('|'))
        .Select(fields => new object[] { fields[0], fields[1], int.Parse(fields[2]), int.Parse(fields[3]), int.Parse(fields[4]) });

    private static readonly Lazy<IReadOnlyCollection<ExecutableTarget>> Targets = new(() => CreateTargets(new TestBuild()));

    [Fact]
    public void AllComponents_ExposeExpectedTargets()
    {
        Targets.Value.Select(target => target.Name).Should().BeEquivalentTo(TargetCases.Select(row => (string)row[0]));
    }

    [Theory]
    [MemberData(nameof(TargetCases))]
    public void Target_HasExpectedDependenciesAndGuards(string name, string dependencies, int actions, int conditions, int requirements)
    {
        var target = Targets.Value.Single(t => t.Name == name);
        target.ExecutionDependencies.Select(t => t.Name).Should().BeEquivalentTo(dependencies.Split(',', StringSplitOptions.RemoveEmptyEntries));
        target.Actions.Should().HaveCount(actions);
        target.DynamicConditions.Should().HaveCount(conditions);
        target.DelegateRequirements.Should().HaveCount(requirements);
    }

    [Fact]
    public void ReleaseAndSetupTargets_KeepTheirOrderingAndTriggers()
    {
        ExecutableTarget Get(string name) => Targets.Value.Single(t => t.Name == name);
        Get("WriteBuildMetadata").OrderDependencies.Select(t => t.Name).Should().Contain("Clean");
        Get("AwsCliPackage").OrderDependencies.Select(t => t.Name).Should().Contain("Test");
        Get("PublishLambdaCI").OrderDependencies.Select(t => t.Name).Should().Contain("Test");
        Get("PackCodeDeploy").Triggers.Select(t => t.Name).Should().Contain("AnnounceCodeDeployArtifactsToTeamCity");
        Get("SetupLocal").Triggers.Select(t => t.Name).Should().BeEquivalentTo("SetUserSecrets", "GenerateAppSettings", "ConfigureLocalCloudformationParameters");
        Get("ValidateDeploymentAcceptance").Triggers.Select(t => t.Name).Should().BeEquivalentTo("EndToEndTest", "ReportFailure");
        foreach (var name in new[] { "BuildRelease", "BuildReleaseCodeDeploy", "DeployArtifacts" })
            Get(name).Triggers.Select(t => t.Name).Should().Contain("ReportFailure");
        Get("ReportFailure").AssuredAfterFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LambdaTargets_OnlyRunForTheirConfiguredDeploymentVersion(bool v2)
    {
        var targets = CreateTargets(new TestBuild { UseV2Deployment = v2 });
        bool Condition(string name) => targets.Single(t => t.Name == name).DynamicConditions.Single().Item2();
        Condition("PublishLambda").Should().Be(!v2);
        Condition("AwsCliPackage").Should().Be(!v2);
        Condition("PublishLambdaCI").Should().Be(v2);
    }

    [Fact]
    public void Clean_OnlyCleansBuildOutputsInsideTheConfiguredDirectories()
    {
        using var root = new TemporaryRoot();
        var sourceOutput = root.File("src/Example/bin/output", "build");
        var testOutput = root.File("tests/Unit/obj/output", "build");
        var artifact = root.File("artifacts/old.txt", "artifact");
        var source = root.File("src/Example/source.cs", "source");
        var build = root.Build();
        CreateTargets(build).Single(t => t.Name == "Clean").Actions.Single()();
        File.Exists(sourceOutput).Should().BeFalse();
        File.Exists(testOutput).Should().BeFalse();
        File.Exists(artifact).Should().BeFalse();
        File.ReadAllText(source).Should().Be("source");
        Directory.Exists(Path.Combine(root.Path, "artifacts")).Should().BeTrue();
    }

    [Fact]
    public void GitHookTargets_UseBuildParametersAndModifyOnlyTheTemporaryRepository()
    {
        using var root = new TemporaryRoot();
        LocalProcessTasks.Require(root.Path, "git", "init", "-q");
        var build = root.Build();
        build.HookType = "pre-push";
        build.HookDryRun = true;
        build.HookSkipTestRuntime = true;
        var targets = CreateTargets(build);
        var hook = Path.Combine(root.Path, ".git", "hooks", "pre-push");
        void Run(string name) => targets.Single(t => t.Name == name).Actions.Single()();
        Run("EnableGitHook");
        File.Exists(hook).Should().BeFalse();
        build.HookDryRun = false;
        Run("EnableGitHook");
        File.ReadAllText(hook).Should().Contain("lint_fix AUTO_STAGE=true lint test_unit").And.NotContain("test_integration");
        Run("ShowGitHooks");
        Run("DisableGitHook");
        File.Exists(hook).Should().BeFalse();
    }

    [Fact]
    public void ValidateCodeDeploySettings_RejectsEmptyArtifactsAndAcceptsEnabledArtifact()
    {
        var build = new TestBuild();
        var validate = CreateTargets(build).Single(t => t.Name == "ValidateCodeDeploySettings").Actions.Single();
        validate.Should().Throw<Exception>().WithMessage("*None of the projects are configured*");
        build.ArtifactList.Add(new Artifact { CodeDeploy = { ArchiveName = "app" } });
        validate.Should().NotThrow();
    }

    [Fact]
    public void GenerateLocalRelease_BumpsSelectedProjectAndDependents()
    {
        using var root = new TemporaryRoot();
        var core = root.File("src/Core/Core.csproj", "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>");
        var app = root.File("src/App/App.csproj", "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup><ItemGroup><ProjectReference Include='../Core/Core.csproj'/></ItemGroup></Project>");
        var build = root.Build();
        build.ReleaseProject = "Core";
        build.ReleaseBump = "minor";
        CreateTargets(build).Single(t => t.Name == "GenerateLocalRelease").Actions.Single()();
        File.ReadAllText(core).Should().Contain("<Version>1.1.0</Version>");
        File.ReadAllText(app).Should().Contain("<Version>1.1.0</Version>");
    }

    [Fact]
    public void SetupBoilerplate_UsesSelectedItems()
    {
        using var root = new TemporaryRoot();
        var build = root.Build();
        build.BoilerplateItems = ".gitignore";
        CreateTargets(build).Single(t => t.Name == "SetupBoilerplate").Actions.Single()();
        File.Exists(Path.Combine(root.Path, ".gitignore")).Should().BeTrue();
        File.Exists(Path.Combine(root.Path, "Makefile")).Should().BeFalse();
    }

    [Fact]
    public void GenerateAppSettings_InCiWritesDefaultsWithoutPrompting()
    {
        using var root = new TemporaryRoot();
        root.File("src/App/appsettings.json.template", "{\"Port\": 5000, \"Secret\": \"enter a secret\"}");
        var build = root.Build();
        build.IsCiBuild = true;
        CreateTargets(build).Single(t => t.Name == "GenerateAppSettings").Actions.Single()();
        var content = File.ReadAllText(Path.Combine(root.Path, "src/App/appsettings.Development.json"));
        content.Should().Contain("5000").And.NotContain("enter a secret");
    }

    [Fact]
    public void PrintArtifactsJson_UsesConfiguredOutputAndDeploymentTypes()
    {
        using var root = new TemporaryRoot();
        var build = root.Build();
        build.ArtifactsJsonOutput = Path.Combine(root.Path, "artifacts.json");
        build.ArtifactList.Add(new Artifact { ProjectFile = AbsolutePath.Create(root.File("src/App/App.csproj", "<Project/>")), CodeDeploy = { ArchiveName = "app" } });
        CreateTargets(build).Single(t => t.Name == "PrintArtifactsJson").Actions.Single()();
        File.ReadAllText(build.ArtifactsJsonOutput).Should().Contain("CodeDeploy").And.Contain("app");
    }

    [Fact]
    public void PackCodeDeploy_CreatesArchiveForEnabledArtifactsOnly()
    {
        using var root = new TemporaryRoot();
        var build = root.Build();
        build.ArtifactList.Add(new Artifact { ProjectFile = AbsolutePath.Create(root.File("src/App/App.csproj", "<Project/>")), CodeDeploy = { ArchiveName = "app" } });
        build.ArtifactList.Add(new Artifact { ProjectFile = AbsolutePath.Create(root.File("src/Skipped/Skipped.csproj", "<Project/>")) });
        root.File("artifacts/App/linux-x64/app.txt", "published");
        Directory.CreateDirectory(Path.Combine(root.Path, "artifacts", "packaged"));
        CreateTargets(build).Single(t => t.Name == "PackCodeDeploy").Actions.Single()();
        using var zip = ZipFile.OpenRead(Path.Combine(root.Path, "artifacts", "packaged", "app.zip"));
        zip.Entries.Select(e => e.FullName).Should().Contain("app.txt");
        Directory.GetFiles(Path.Combine(root.Path, "artifacts", "packaged")).Should().HaveCount(1);
    }

    [Fact]
    public void SolutionTargets_HandleAnEmptySolutionWithoutExternalTools()
    {
        using var root = new TemporaryRoot();
        Directory.CreateDirectory(Path.Combine(root.Path, "src"));
        Directory.CreateDirectory(Path.Combine(root.Path, "docs"));
        var build = root.Build();
        build.DependencyMapOutput = AbsolutePath.Create(Path.Combine(root.Path, "docs", "map.html"));
        var targets = CreateTargets(build);
        void Run(string name) => targets.Single(t => t.Name == name).Actions.Single()();
        Run("ListProjects");
        Run("ListPackages");
        Run("CheckLayering");
        Run("GenerateDependencyMap");
        Run("TestUnit");
        Run("TestIntegration");
        Run("TestEndToEnd");
        File.ReadAllText(build.DependencyMapOutput).Should().Contain("<html");
    }

    [Fact]
    public void AppTargets_RejectMissingNamesWithoutStartingAnyProcess()
    {
        var build = new TestBuild();
        var targets = CreateTargets(build);
        targets.Single(t => t.Name == "RunApp").Actions.Single().Should().Throw<Exception>().WithMessage("*AppName*");
        targets.Single(t => t.Name == "RunApps").Actions.Single().Should().Throw<Exception>().WithMessage("*AppNames*");
    }

    [Fact]
    public void UpgradeDependencies_RejectsInvalidLockBeforeStartingAnyTool()
    {
        var build = new TestBuild { DependencyVersionLock = "invalid" };
        CreateTargets(build).Single(t => t.Name == "UpgradeDependencies").Actions.Single()
            .Should().Throw<ArgumentException>().WithMessage("*version lock*");
    }

    [Fact]
    public void ReleaseRequirements_TrackTheValuesProvidedToTheBuild()
    {
        var build = new TestBuild();
        var targets = CreateTargets(build);
        object?[] Requirements(string name) => targets.Single(t => t.Name == name).DelegateRequirements
            .Select(requirement => requirement.Compile().DynamicInvoke()).ToArray();
        Requirements("GenerateLocalRelease").Should().OnlyContain(value => value == null);
        foreach (var name in new[] { "GenerateReleaseFromChanges", "GenerateReleaseNotes", "ValidateLocalRelease", "Publish" })
            Requirements(name).Should().ContainSingle().Which.Should().BeNull();
        build.ReleaseProject = "Core";
        build.ReleaseBump = "patch";
        build.ReleaseBase = "main";
        build.NuGetSource = "local-feed";
        Requirements("GenerateLocalRelease").Should().BeEquivalentTo(new object?[] { "Core", "patch" });
        foreach (var name in new[] { "GenerateReleaseFromChanges", "GenerateReleaseNotes", "ValidateLocalRelease" })
            Requirements(name).Should().ContainSingle().Which.Should().Be("main");
        Requirements("Publish").Should().ContainSingle().Which.Should().Be("local-feed");
    }

    [Fact]
    public void ServerOnlyTargets_UseTheBuildHostCondition()
    {
        var build = new TestBuild();
        var targets = CreateTargets(build);
        foreach (var name in new[] { "DeployServer", "SetTestEnvironmentVariables", "SetEndToEndTestEnvironmentVariables", "AnnounceCodeDeployArtifactsToTeamCity" })
            targets.Single(t => t.Name == name).DynamicConditions.Single().Item2().Should().Be(((INukeBuild)build).IsServerBuild);
    }

    [Fact]
    public void DefaultBuild_KeepsItsOriginalTestAndPullRequestDependencies()
    {
        var targets = CreateTargets(new DefaultTestBuild());
        targets.Single(t => t.Name == "Test").ExecutionDependencies.Select(t => t.Name)
            .Should().BeEquivalentTo("Build", "SetTestEnvironmentVariables");
        targets.Single(t => t.Name == "PullRequest").ExecutionDependencies.Select(t => t.Name)
            .Should().BeEquivalentTo("Build", "Test");
        targets.Single(t => t.Name == "SetupLocal").Triggers.Select(t => t.Name).Should().ContainSingle().Which.Should().Be("SetUserSecrets");
    }

    private static IReadOnlyCollection<ExecutableTarget> CreateTargets<T>(T build) where T : NukeBuild
    {
        var factory = typeof(NukeBuild).Assembly.GetType("Nuke.Common.Execution.ExecutableTargetFactory", throwOnError: true)!;
        var create = factory.GetMethod("CreateAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        return (IReadOnlyCollection<ExecutableTarget>)create.Invoke(null, [build, Array.Empty<Expression<Func<T, Target>>>()])!;
    }

    private sealed class DefaultTestBuild : NukeBuild, IDefaultBuild
    {
        public IReadOnlyCollection<Artifact> Artifacts => [];
    }

    private sealed class TestBuild : NukeBuild, IFullReleaseBuild, IContainerBuild, IHasEndToEndTests,
        IReportFailureOnSlack, INuGetBuild, ILayeringBuild, IDependencyMapBuild, IBoilerplateBuild,
        IDependencyUpgradeBuild, IGitHooksBuild, INukeBuild
    {
        public AbsolutePath TestRoot { get; set; } = AbsolutePath.Create(Directory.GetCurrentDirectory());
        AbsolutePath INukeBuild.RootDirectory => TestRoot;
        public Solution Solution { get; set; } = new(new SolutionModel(), AbsolutePath.Create(Path.Combine(Directory.GetCurrentDirectory(), "test.sln")));
        public List<Artifact> ArtifactList { get; } = [];
        public IReadOnlyCollection<Artifact> Artifacts => ArtifactList;
        public string ContainerRegistry => "example.invalid";
        public IReadOnlyDictionary<string, ConfigurationValue> EndToEndTestConfiguration => new Dictionary<string, ConfigurationValue>();
        public string ApplicationName => "test";
        public IReadOnlyDictionary<BuildEnvironment, Secret> EncryptedSlackWebhookUrls => new Dictionary<BuildEnvironment, Secret>();
        public bool UseV2Deployment { get; set; }
        public bool IsCiBuild { get; set; }
        public string? HookType { get; set; }
        public bool HookDryRun { get; set; }
        public bool HookSkipTestRuntime { get; set; }
        public string? ReleaseProject { get; set; }
        public string? ReleaseBump { get; set; }
        public string? ReleaseBase { get; set; }
        public string? NuGetSource { get; set; }
        public string? BoilerplateItems { get; set; }
        public string? ArtifactsJsonOutput { get; set; }
        public AbsolutePath DependencyMapOutput { get; set; } = AbsolutePath.Create(Path.Combine(Directory.GetCurrentDirectory(), "map.html"));
        public string DependencyVersionLock { get; set; } = "Major";
    }

    private sealed class TemporaryRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nuke-target-" + Guid.NewGuid().ToString("N"));
        public TemporaryRoot() => Directory.CreateDirectory(Path);
        public string File(string relativePath, string content)
        {
            var path = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllText(path, content);
            return path;
        }
        public TestBuild Build()
        {
            var build = new TestBuild();
            build.TestRoot = AbsolutePath.Create(Path);
            build.Solution = new Solution(new SolutionModel(), AbsolutePath.Create(System.IO.Path.Combine(Path, "test.sln")));
            return build;
        }
        public void Dispose() => Directory.Delete(Path, true);
    }
}
