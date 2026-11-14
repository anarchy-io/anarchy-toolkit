using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Fallout.Component;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using AwesomeAssertions;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Persistence.Solution.Model;
using Fallout.Solutions;
using System.Linq.Expressions;
using System.Reflection;


namespace Anarchy.Toolkit.Tests.Unit.Build.Fallout.Component;

public class TargetGraphTest
{
    // Exercise Fallout's actual component resolution without running any publishing or deployment tools.
    // Each row is target | direct dependencies | action count | dynamic conditions | requirements.
    private const string TargetSpecifications = """
        Build|Clean|1|0|0
        CheckLayering||1|0|0
        Clean||1|0|0
        Compile|Restore|1|0|0
        DisableGitHook||1|0|0
        DockerDestroy||1|0|0
        DockerInfo||1|0|0
        DockerLogs||1|0|0
        DockerRestart||1|0|0
        DockerStart||1|0|0
        DockerStop||1|0|0
        DockerStopAll||1|0|0
        DockerWaitHealthy||1|0|1
        DockerWaitRunning||1|0|1
        EcrImageBuild|EcrPublishAppArtifact|1|0|0
        EcrImagePush|EcrImageBuild|1|0|0
        EcrPublishAppArtifact|Compile|1|0|2
        EnableGitHook||1|0|0
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
        Publish|Pack|1|0|1
        PullRequest|Lint,CheckLayering,TestAll|0|0|0
        Restore||1|0|0
        RunApp||1|0|0
        RunApps||1|0|0
        SetupBoilerplate||1|0|0
        ShowGitHooks||1|0|0
        TestAll|TestUnit,TestIntegration,TestEndToEnd|0|0|0
        TestEndToEnd|Compile|1|0|0
        TestIntegration|Compile|1|0|0
        TestUnit|Compile|1|0|0
        UpgradeDependencies||1|0|0
        ValidateLocalRelease||1|0|1
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
    public void ToolVerbosity_IsMinimalUnlessTheBuildRunsOnCi()
    {
        var build = new TestBuild();
        ((IDefaultBuild)build).ToolVerbosity.Should().Be(DotNetVerbosity.minimal);
        build.IsCiBuild = true;
        ((IDefaultBuild)build).ToolVerbosity.Should().Be(DotNetVerbosity.diagnostic);
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

    private static IReadOnlyCollection<ExecutableTarget> CreateTargets<T>(T build) where T : FalloutBuild
    {
        var factory = typeof(FalloutBuild).Assembly.GetType("Fallout.Common.Execution.ExecutableTargetFactory", throwOnError: true)!;
        var create = factory.GetMethod("CreateAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        return (IReadOnlyCollection<ExecutableTarget>)create.Invoke(null, [build, Array.Empty<Expression<Func<T, Target>>>()])!;
    }

    private sealed class DefaultTestBuild : FalloutBuild, IDefaultBuild
    {
        public IReadOnlyCollection<Artifact> Artifacts => [];
    }

    private sealed class TestBuild : FalloutBuild, IContainerBuild, INuGetBuild, ILayeringBuild,
        IDependencyMapBuild, IBoilerplateBuild, IDependencyUpgradeBuild, IGitHooksBuild, IDockerBuild, IFalloutBuild
    {
        public AbsolutePath TestRoot { get; set; } = AbsolutePath.Create(Directory.GetCurrentDirectory());
        AbsolutePath IFalloutBuild.RootDirectory => TestRoot;
        public Solution Solution { get; set; } = new(new SolutionModel(), AbsolutePath.Create(Path.Combine(Directory.GetCurrentDirectory(), "test.sln")));
        public List<Artifact> ArtifactList { get; } = [];
        public IReadOnlyCollection<Artifact> Artifacts => ArtifactList;
        public string ContainerRegistry => "example.invalid";
        public bool IsCiBuild { get; set; }
        public string? HookType { get; set; }
        public bool HookDryRun { get; set; }
        public bool HookSkipTestRuntime { get; set; }
        public string? ReleaseProject { get; set; }
        public string? ReleaseBump { get; set; }
        public string? ReleaseBase { get; set; }
        public string? NuGetSource { get; set; }
        public string? BoilerplateItems { get; set; }
        public AbsolutePath DependencyMapOutput { get; set; } = AbsolutePath.Create(Path.Combine(Directory.GetCurrentDirectory(), "map.html"));
        public string DependencyVersionLock { get; set; } = "Major";
    }

    private sealed class TemporaryRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fallout-target-" + Guid.NewGuid().ToString("N"));
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
