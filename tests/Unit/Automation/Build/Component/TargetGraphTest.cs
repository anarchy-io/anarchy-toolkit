using Anarchy.Toolkit.Automation.Build.Component;
using Anarchy.Toolkit.Automation.Build.Core.Entity.Build;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Persistence.Solution.Model;
using Fallout.Solutions;
using System.Linq.Expressions;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Component;

public class TargetGraphTest
{
    // Exercise Fallout's actual component resolution without running any publishing or deployment tools.
    // Each row is target | direct dependencies | action count | dynamic conditions | requirements.
    private const string TargetSpecifications = """
                                                Build|Clean|1|0|0
                                                CheckLayering||1|0|0
                                                Clean||1|0|0
                                                Compile|Restore|1|0|0
                                                DiffBoilerplate||1|0|0
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
                                                ContainerImageBuild|ContainerPublishAppArtifact|1|0|1
                                                ContainerImagePush|ContainerImageBuild|1|0|0
                                                ContainerPublishAppArtifact|Compile,TestUnit|1|0|2
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
                                                MockServerRun||1|0|0
                                                MockServerStart|Compile|1|0|0
                                                MockServerStop||1|0|0
                                                MockServerSyncOpenApi||1|0|1
                                                Pack|Compile,TestUnit|1|0|0
                                                Publish|Pack|1|0|1
                                                PullRequest|Lint,CheckLayering,TestAll|0|0|0
                                                Restore||1|0|0
                                                RunApp||1|0|0
                                                RunApps||1|0|0
                                                SetupBoilerplate||1|0|0
                                                ShowAnarchyPreferences||1|0|0
                                                SetAnarchyPreference||1|0|2
                                                ShowGitHooks||1|0|0
                                                TestAll|TestUnit,TestIntegration,TestEndToEnd|0|0|0
                                                TestEndToEnd|Compile|1|0|0
                                                TestEndToEndWithMockServer|Compile|1|0|0
                                                TestIntegration|Compile|1|0|0
                                                TestIntegrationWithMockServer|Compile|1|0|0
                                                TestUnit|Compile|1|0|0
                                                UnsetAnarchyPreference||1|0|1
                                                ConfigureAnarchyPreferences||1|0|0
                                                UpgradeDependencies||1|0|0
                                                ValidateLocalRelease||1|0|1
                                                """;

    public static IEnumerable<object[]> TargetCases => TargetSpecifications
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim()
            .Split('|'))
        .Select(fields => new object[] {
            fields[0],
            fields[1],
            int.Parse(fields[2]),
            int.Parse(fields[3]),
            int.Parse(fields[4])
        });

    private static readonly Lazy<IReadOnlyCollection<ExecutableTarget>> Targets = new(() =>
        CreateTargets(new TestBuild()));

    [Fact]
    public void AnarchyBuild_ProvidesAnEngineCompatibleComponentHost()
    {
        typeof(AnarchyBuild).IsAbstract.Should()
            .BeTrue();

        typeof(IFalloutBuild).IsAssignableFrom(typeof(AnarchyBuild))
            .Should()
            .BeTrue();

        var host = new AnarchyComponentTestBuild();

        CreateTargets(host)
            .Select(target => target.Name)
            .Should()
            .Contain("Clean");
    }

    [Fact]
    public void AllComponents_ExposeExpectedTargets()
    {
        Targets.Value.Select(target => target.Name)
            .Should()
            .BeEquivalentTo(TargetCases.Select(row => (string)row[0]));
    }

    [Theory]
    [MemberData(nameof(TargetCases))]
    public void Target_HasExpectedDependenciesAndGuards(string name, string dependencies, int actions, int conditions,
                                                         int requirements)
    {
        var target = Targets.Value.Single(t => t.Name == name);
        target.ExecutionDependencies.Select(t => t.Name)
            .Should()
            .BeEquivalentTo(dependencies.Split(',', StringSplitOptions.RemoveEmptyEntries));

        target.Actions.Should()
            .HaveCount(actions);

        target.DynamicConditions.Should()
            .HaveCount(conditions);

        target.DelegateRequirements.Should()
            .HaveCount(requirements);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TestEnvironment_PreparesOnlyEnvironmentSuitesAndOrdersOptionalMigrationAfterReadiness(bool withFlyway)
    {
        var targets = withFlyway ? CreateTargets(new ProvisionedTestBuild()) : CreateTargets(new EnvironmentTestBuild());

        IEnumerable<string> Dependencies(string name) => targets.Single(t => t.Name == name)
            .ExecutionDependencies.Select(t => t.Name);

        foreach (var target in new[] { "TestIntegration", "TestEndToEnd", "TestIntegrationWithMockServer", "TestEndToEndWithMockServer" })
            Dependencies(target).Should().BeEquivalentTo("Compile", "PrepareTestEnvironment");

        Dependencies("TestUnit").Should().Equal("Compile");
        Dependencies("DockerStart").Should().BeEmpty();
        if (withFlyway)
        {
            Dependencies("PrepareTestEnvironment").Should().BeEquivalentTo("DockerStart", "FlywayMigrate");
            Dependencies("FlywayMigrate").Should().Equal("DockerStart");
            Dependencies("FlywayInfo").Should().BeEmpty();
        }
        else
        {
            Dependencies("PrepareTestEnvironment").Should().Equal("DockerStart");
            targets.Select(t => t.Name).Should().NotContain(["FlywayMigrate", "FlywayInfo"]);
        }
    }

    [Fact]
    public void FlywayTargets_WithoutTestEnvironment_DoNotStartComposeOrChangeTheTestGraph()
    {
        var targets = CreateTargets(new StandaloneFlywayTestBuild());

        foreach (var target in new[] { "FlywayMigrate", "FlywayInfo" })
            targets.Single(t => t.Name == target).ExecutionDependencies.Should().BeEmpty();

        foreach (var target in new[] { "TestUnit", "TestIntegration", "TestEndToEnd" })
            targets.Single(t => t.Name == target).ExecutionDependencies.Select(t => t.Name).Should().Equal("Compile");
    }

    [Theory]
    [InlineData("Pack")]
    [InlineData("Publish")]
    [InlineData("ContainerPublishAppArtifact")]
    [InlineData("ContainerImageBuild")]
    [InlineData("ContainerImagePush")]
    public void DeliveryTargets_RunUnitTestsButNoTestsThatNeedAnEnvironment(string name)
    {
        var reachable = new HashSet<string>();
        var pending = new Stack<ExecutableTarget>([Targets.Value.Single(t => t.Name == name)]);
        while (pending.TryPop(out var target))
            foreach (var dependency in target.ExecutionDependencies.Concat(target.Triggers))
                if (reachable.Add(dependency.Name))
                    pending.Push(dependency);

        reachable.Should()
            .Contain("TestUnit")
            .And.NotContain([
                "TestIntegration", "TestEndToEnd", "TestAll",
                "TestIntegrationWithMockServer", "TestEndToEndWithMockServer", "PullRequest"
            ]);
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
        CreateTargets(build)
            .Single(t => t.Name == "Clean")
            .Actions.Single()();

        File.Exists(sourceOutput)
            .Should()
            .BeFalse();

        File.Exists(testOutput)
            .Should()
            .BeFalse();

        File.Exists(artifact)
            .Should()
            .BeFalse();

        File.ReadAllText(source)
            .Should()
            .Be("source");

        Directory.Exists(Path.Combine(root.Path, "artifacts"))
            .Should()
            .BeTrue();
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

        void Run(string name) => targets.Single(t => t.Name == name)
            .Actions.Single()();

        Run("EnableGitHook");
        File.Exists(hook)
            .Should()
            .BeFalse();

        build.HookDryRun = false;
        Run("EnableGitHook");
        File.ReadAllText(hook)
            .Should()
            .Contain("lint_fix AUTO_STAGE=true lint test_unit")
            .And.NotContain("test_integration");

        Run("ShowGitHooks");
        Run("DisableGitHook");
        File.Exists(hook)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void GenerateLocalRelease_BumpsSelectedProjectAndDependents()
    {
        using var root = new TemporaryRoot();
        var core = root.File("src/Core/Core.csproj",
            "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>");

        var app = root.File("src/App/App.csproj",
            "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup><ItemGroup><ProjectReference Include='../Core/Core.csproj'/></ItemGroup></Project>");

        var build = root.Build();
        build.ReleaseProject = "Core";
        build.ReleaseBump = "minor";
        CreateTargets(build)
            .Single(t => t.Name == "GenerateLocalRelease")
            .Actions.Single()();

        File.ReadAllText(core)
            .Should()
            .Contain("<Version>1.1.0</Version>");

        File.ReadAllText(app)
            .Should()
            .Contain("<Version>1.1.0</Version>");
    }

    [Fact]
    public void SetupBoilerplate_UsesSelectedItems()
    {
        using var root = new TemporaryRoot();
        var build = root.Build();
        build.BoilerplateItems = ".gitignore";
        CreateTargets(build)
            .Single(t => t.Name == "SetupBoilerplate")
            .Actions.Single()();

        File.Exists(Path.Combine(root.Path, ".gitignore"))
            .Should()
            .BeTrue();

        File.Exists(Path.Combine(root.Path, "Makefile"))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void SourceAndTestsRoots_DefaultToTheToolkitFoldersAndCanBeConfigured()
    {
        var build = new TestBuild();
        var standard = (IStandardBuild)build;
        standard.SourceDirectory.Should()
            .Be(build.TestRoot / "src");

        standard.TestsDirectory.Should()
            .Be(build.TestRoot / "tests");

        build.SourceRoot = "code";
        build.TestsRoot = Path.Combine("quality", "tests");
        standard.SourceDirectory.Should()
            .Be(build.TestRoot / "code");

        standard.TestsDirectory.Should()
            .Be(build.TestRoot / "quality" / "tests");
    }

    [Fact]
    public void Preferences_ReadTheUnifiedFileAtTheSolutionRoot()
    {
        using var root = new TemporaryRoot();
        var build = new PreferenceTestBuild(root.Path);
        root.File(".anarchy-toolkit.json", """{ "build": { "sourceRoot": "code" } }""");

        ((IDefaultBuild)build).SourceDirectory.Should()
            .Be(build.Root / "code");

        ((IAutomationPreferencesBuild)build).PreferencePath("build.sourceRoot")
            .Should()
            .Be(build.Root / "code");
    }

    [Fact]
    public void LayeringTargets_UseDefaultsWithoutWritingAndReadSavedOverrides()
    {
        using var root = new TemporaryRoot();
        Directory.CreateDirectory(Path.Combine(root.Path, "src"));
        var build = root.Build();
        build.DependencyMapOutput = AbsolutePath.Create(Path.Combine(root.Path, "map.html"));
        var targets = CreateTargets(build);

        Action Run(string name) => targets.Single(t => t.Name == name)
            .Actions.Single();

        Run("CheckLayering")
            .Should()
            .NotThrow();

        Run("GenerateDependencyMap")
            .Should()
            .NotThrow();

        File.Exists(Path.Combine(root.Path, ".anarchy-toolkit.json"))
            .Should()
            .BeFalse();

        root.File(".anarchy-toolkit.json",
            """{ "layering": { "mode": "folder", "libraryRoot": "src/Library", "layers": ["Core", "Web"], "applicationFolders": {} } }""");

        ((ILayeringBuild)build).Layering.Should()
            .BeEquivalentTo(new SolutionLayout(LayeringMode.Folder, ["Core", "Web"]) {
                ApplicationFolders = new Dictionary<string, string>()
            });

        Run("CheckLayering")
            .Should()
            .NotThrow();

        Run("GenerateDependencyMap")
            .Should()
            .NotThrow();

        File.ReadAllText(build.DependencyMapOutput)
            .Should()
            .Contain("Core &lt; Web");
    }

    [Fact]
    public void ToolVerbosity_IsMinimalUnlessTheBuildRunsOnCi()
    {
        using var root = new TemporaryRoot();
        var build = new PreferenceTestBuild(root.Path);
        ((IDefaultBuild)build).ToolVerbosity.Should()
            .Be(DotNetVerbosity.minimal);

        build.IsCiBuild = true;
        ((IDefaultBuild)build).ToolVerbosity.Should()
            .Be(DotNetVerbosity.diagnostic);
    }

    [Fact]
    public void QuietBuildVerbosity_SuppressesInformationalTargetOutputAndToolOutput()
    {
        var build = new TestBuild {
            IsCiBuild = true
        };

        ((IDefaultBuild)build).ToolVerbosity.Should()
            .Be(DotNetVerbosity.quiet);

        ((IFalloutBuild)build).Verbosity.Should()
            .Be(Fallout.Common.Verbosity.Quiet);
    }

    [Fact]
    public void GenerateAppSettings_InCiWritesDefaultsWithoutPrompting()
    {
        using var root = new TemporaryRoot();
        root.File("src/App/appsettings.json.template", "{\"Port\": 5000, \"Secret\": \"enter a secret\"}");
        var build = root.Build();
        build.IsCiBuild = true;
        CreateTargets(build)
            .Single(t => t.Name == "GenerateAppSettings")
            .Actions.Single()();

        var content = File.ReadAllText(Path.Combine(root.Path, "src/App/appsettings.Development.json"));
        content.Should()
            .Contain("5000")
            .And.NotContain("enter a secret");
    }

    [Fact]
    public void SolutionTargets_HandleAnEmptySolutionWithoutExternalTools()
    {
        using var root = new TemporaryRoot();
        Directory.CreateDirectory(Path.Combine(root.Path, "src"));
        Directory.CreateDirectory(Path.Combine(root.Path, "docs"));
        root.File(".anarchy-toolkit.json", """{ "layering": { "mode": "prefix" } }""");
        var build = root.Build();
        build.DependencyMapOutput = AbsolutePath.Create(Path.Combine(root.Path, "docs", "map.html"));
        var targets = CreateTargets(build);

        void Run(string name) => targets.Single(t => t.Name == name)
            .Actions.Single()();

        Run("ListProjects");
        Run("ListPackages");
        Run("CheckLayering");
        Run("GenerateDependencyMap");
        Run("TestUnit");
        Run("TestIntegration");
        Run("TestEndToEnd");
        File.ReadAllText(build.DependencyMapOutput)
            .Should()
            .Contain("<html");
    }

    [Fact]
    public void LayeringInvocationParameters_OverrideSavedPreferencesAndDefaults()
    {
        using var root = new TemporaryRoot();
        root.File(".anarchy-toolkit.json", """
                                           { "layering": { "mode": "prefix", "layers": ["Core", "Web"], "root": "code" } }
                                           """);

        var build = root.Build();
        var layering = (ILayeringBuild)build;

        layering.Layering.Mode.Should()
            .Be(LayeringMode.Prefix);

        layering.Layering.Root.Should()
            .Be("code");

        build.LayeringModeOverride = "folder";
        build.LayeringRoot = "source";
        build.LayeringLibraryRoot = "source/Libraries";
        build.LayeringApplicationFolders = "{ \"source/Api\": \"Web\" }";

        layering.Layering.Should()
            .BeEquivalentTo(new SolutionLayout(LayeringMode.Folder, ["Core", "Web"]) {
                Root = "source",
                LibraryRoot = "source/Libraries",
                ApplicationFolders = new Dictionary<string, string> {
                    ["source/Api"] = "Web"
                }
            });
    }

    [Fact]
    public void PreferenceTargets_SetAndUnsetTypedOverridesWithoutOrdinaryTargetsWriting()
    {
        using var root = new TemporaryRoot();
        var build = root.Build();
        var targets = CreateTargets(build);

        Action Run(string name) => targets.Single(t => t.Name == name)
            .Actions.Single();

        Run("ShowAnarchyPreferences")();
        File.Exists(Path.Combine(root.Path, ".anarchy-toolkit.json"))
            .Should()
            .BeFalse();

        Run("CheckLayering")();
        File.Exists(Path.Combine(root.Path, ".anarchy-toolkit.json"))
            .Should()
            .BeFalse();

        build.PreferenceKey = "docker.timeout";
        build.PreferenceValue = "45";
        Run("SetAnarchyPreference")();
        AutomationPreferences.Read(root.Path)
            .Get<int?>("docker.timeout")
            .Should()
            .Be(45);

        build.PreferenceValue = "false";
        build.PreferenceKey = "gitHooks.skipTestRuntime";
        Run("SetAnarchyPreference")();
        AutomationPreferences.Read(root.Path)
            .Get<bool?>("gitHooks.skipTestRuntime")
            .Should()
            .BeFalse();

        build.PreferenceKey = "docker.timeout";
        Run("UnsetAnarchyPreference")();
        AutomationPreferences.Read(root.Path)
            .Get<int?>("docker.timeout")
            .Should()
            .BeNull();

        AutomationPreferences.Read(root.Path)
            .Get<bool?>("gitHooks.skipTestRuntime")
            .Should()
            .BeFalse();
    }

    [Fact]
    public void PreferenceResolution_UsesSavedValuesBeforeToolkitDefaults()
    {
        using var root = new TemporaryRoot();
        root.File(".anarchy-toolkit.json", """
                                           {
                                             "build": { "sourceRoot": "production", "testsRoot": "quality", "treatWarningsAsErrors": false },
                                             "docker": { "waitFor": ["postgres"], "waitForHealthy": true, "timeout": 24 },
                                             "gitHooks": { "skipTestRuntime": true }
                                           }
                                           """);

        var build = new PreferenceTestBuild(root.Path);
        var standard = (IStandardBuild)build;
        var docker = (IDockerBuild)build;
        var hooks = (IGitHooksBuild)build;

        standard.SourceDirectory.Should()
            .Be(build.Root / "production");

        standard.TestsDirectory.Should()
            .Be(build.Root / "quality");

        ((IDefaultBuild)build).TreatWarningsAsErrors.Should()
            .BeFalse();

        docker.DockerWaitFor.Should()
            .Equal("postgres");

        docker.DockerWaitForHealthy.Should()
            .BeTrue();

        docker.DockerTimeout.Should()
            .Be(24);

        hooks.HookSkipTestRuntime.Should()
            .BeTrue();
    }

    [Fact]
    public void PublicDefaults_MatchTheReviewedSourceDefaultContract()
    {
        using var root = new TemporaryRoot();
        var build = new PreferenceTestBuild(root.Path);
        var standard = (IStandardBuild)build;
        var defaults = (IDefaultBuild)build;
        var docker = (IDockerBuild)build;
        var hooks = (IGitHooksBuild)build;

        defaults.Configuration.Should()
            .Be(Anarchy.Toolkit.Automation.Build.Core.ValueSet.Build.ConfigurationValueSet.Release);

        standard.SourceDirectory.Should()
            .Be(build.Root / "src");

        standard.TestsDirectory.Should()
            .Be(build.Root / "tests");

        defaults.TreatWarningsAsErrors.Should()
            .BeTrue();

        defaults.ToolVerbosity.Should()
            .Be(Fallout.Common.Tools.DotNet.DotNetVerbosity.minimal);

        build.IsCiBuild = true;
        defaults.ToolVerbosity.Should()
            .Be(Fallout.Common.Tools.DotNet.DotNetVerbosity.diagnostic);

        ((ILayeringBuild)build).Layering.Should()
            .BeEquivalentTo(SolutionLayout.Default);

        docker.DockerComposeFile.Should()
            .Be(build.Root / "docker" / "docker-compose.yml");

        ((IContainerBuild)build).Dockerfile.Should()
            .Be(build.Root / "docker" / "Dockerfile");

        ((IContainerBuild)build).ContainerAppArtifactDirectory.Should()
            .Be(build.Root / "artifacts" / "Container");

        docker.DockerWaitFor.Should()
            .BeEmpty();

        docker.DockerWaitForHealthy.Should()
            .BeFalse();

        docker.DockerTimeout.Should()
            .Be(60);

        docker.DockerStartTimeout.Should()
            .Be(180);

        hooks.HookSkipLintFix.Should()
            .BeFalse();

        hooks.HookSkipTestStatic.Should()
            .BeFalse();

        hooks.HookSkipTestRuntime.Should()
            .BeFalse();
    }

    [Fact]
    public void ExplicitBuildMembers_PreserveFalseEmptyAndZeroOverrides()
    {
        var build = new TestBuild {
            DockerWaitFor = [],
            DockerWaitForHealthy = false,
            DockerTimeout = 0,
            HookSkipTestRuntime = false
        };

        ((IDockerBuild)build).DockerWaitFor.Should()
            .BeEmpty();

        ((IDockerBuild)build).DockerWaitForHealthy.Should()
            .BeFalse();

        ((IDockerBuild)build).DockerTimeout.Should()
            .Be(0);

        ((IGitHooksBuild)build).HookSkipTestRuntime.Should()
            .BeFalse();
    }

    [Fact]
    public void StandaloneDockerAndHookComponents_ReadPreferencesWithoutLoadingASolution()
    {
        using var root = new TemporaryRoot();
        root.File(".anarchy-toolkit.json", """
                                           { "docker": { "projectName": "local-db", "waitFor": ["postgres"] },
                                             "gitHooks": { "skipTestStatic": true } }
                                           """);

        var build = new StandaloneComponentsBuild(root.Path);

        ((IDockerBuild)build).DockerProjectName.Should()
            .Be("local-db");

        ((IDockerBuild)build).DockerWaitFor.Should()
            .Equal("postgres");

        ((IGitHooksBuild)build).HookSkipTestStatic.Should()
            .BeTrue();
    }

    [Fact]
    public void SkillSync_RejectsInvocationDirectoriesOutsideTheSolutionBeforeWriting()
    {
        using var root = new TemporaryRoot();
        foreach (var directory in new[] {
                     "../outside",
                     "skills/../../outside",
                     "/outside",
                     "C:/outside"
                 })
        {
            var build = new SkillPreferencesBuild(root.Path) {
                AgentSkillsDirectories = [directory]
            };

            var sync = CreateTargets(build)
                .Single(target => target.Name == "SyncAgentSkills")
                .Actions.Single();

            sync.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*agentSkills.directories*inside the solution*");
        }

        Directory.GetFileSystemEntries(root.Path)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void BuildMemberOverrides_ContinueToWinOverSavedPreferences()
    {
        using var root = new TemporaryRoot();
        root.File(".anarchy-toolkit.json", """{ "build": { "sourceRoot": "configured" } }""");
        var build = new BuildWithCustomSource(root.Path);

        ((IDefaultBuild)build).SourceDirectory.Should()
            .Be(build.TestRoot / "custom-source");
    }

    [Fact]
    public void AppTargets_RejectMissingNamesWithoutStartingAnyProcess()
    {
        var build = new TestBuild();
        var targets = CreateTargets(build);
        targets.Single(t => t.Name == "RunApp")
            .Actions.Single()
            .Should()
            .Throw<Exception>()
            .WithMessage("*AppName*");

        targets.Single(t => t.Name == "RunApps")
            .Actions.Single()
            .Should()
            .Throw<Exception>()
            .WithMessage("*AppNames*");
    }

    [Fact]
    public void UpgradeDependencies_RejectsInvalidLockBeforeStartingAnyTool()
    {
        var build = new TestBuild {
            DependencyVersionLock = "invalid"
        };

        CreateTargets(build)
            .Single(t => t.Name == "UpgradeDependencies")
            .Actions.Single()
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*version lock*");
    }

    [Fact]
    public void ReleaseRequirements_TrackTheValuesProvidedToTheBuild()
    {
        var build = new TestBuild();
        var targets = CreateTargets(build);

        object?[] Requirements(string name) => targets.Single(t => t.Name == name)
            .DelegateRequirements
            .Select(requirement => requirement.Compile()
                .DynamicInvoke())
            .ToArray();

        Requirements("GenerateLocalRelease")
            .Should()
            .OnlyContain(value => value == null);

        foreach (var name in new[] {
                     "GenerateReleaseFromChanges",
                     "GenerateReleaseNotes",
                     "ValidateLocalRelease",
                     "Publish"
                 })
            Requirements(name)
                .Should()
                .ContainSingle()
                .Which.Should()
                .BeNull();

        build.ReleaseProject = "Core";
        build.ReleaseBump = "patch";
        build.ReleaseBase = "main";
        build.NuGetSource = "local-feed";
        Requirements("GenerateLocalRelease")
            .Should()
            .BeEquivalentTo(new object?[] {
                "Core",
                "patch"
            });

        foreach (var name in new[] {
                     "GenerateReleaseFromChanges",
                     "GenerateReleaseNotes",
                     "ValidateLocalRelease"
                 })
            Requirements(name)
                .Should()
                .ContainSingle()
                .Which.Should()
                .Be("main");

        Requirements("Publish")
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be("local-feed");
    }

    private static IReadOnlyCollection<ExecutableTarget> CreateTargets<T>(T build) where T : FalloutBuild
    {
        var factory =
            typeof(FalloutBuild).Assembly.GetType("Fallout.Common.Execution.ExecutableTargetFactory",
                throwOnError: true)!;

        var create = factory.GetMethod("CreateAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(T));

        return (IReadOnlyCollection<ExecutableTarget>)create.Invoke(null,
            [build, Array.Empty<Expression<Func<T, Target>>>()])!;
    }

    private sealed class DefaultTestBuild : FalloutBuild, IDefaultBuild
    {
        public IReadOnlyCollection<Artifact> Artifacts => [];
    }

    private sealed class AnarchyComponentTestBuild : AnarchyBuild, IDefaultBuild
    {
        public IReadOnlyCollection<Artifact> Artifacts => [];
    }

    private sealed class BuildWithCustomSource(string root) : FalloutBuild, IStandardBuild
    {
        public AbsolutePath TestRoot { get; } = AbsolutePath.Create(root);
        AbsolutePath IFalloutBuild.RootDirectory => TestRoot;
        AbsolutePath IDefaultBuild.SourceDirectory => TestRoot / "custom-source";
        public IReadOnlyCollection<Artifact> Artifacts => [];
    }

    private sealed class PreferenceTestBuild(string root)
        : FalloutBuild, IContainerBuild, ILayeringBuild, IDockerBuild, IGitHooksBuild
    {
        public AbsolutePath Root { get; } = AbsolutePath.Create(root);
        AbsolutePath IFalloutBuild.RootDirectory => Root;
        public bool IsCiBuild { get; set; }
        public IReadOnlyCollection<Artifact> Artifacts => [];
    }

    private sealed class SkillPreferencesBuild(string root) : FalloutBuild, IAgentSkillsBuild
    {
        AbsolutePath IFalloutBuild.RootDirectory => AbsolutePath.Create(root);
        public IReadOnlyCollection<Artifact> Artifacts => [];
        public string AgentSkillsPrefix => "test";
        public string[] AgentSkillsDirectories { get; set; } = [];
    }

    private sealed class StandaloneComponentsBuild(string root) : FalloutBuild, IDockerBuild, IGitHooksBuild
    {
        AbsolutePath IFalloutBuild.RootDirectory => AbsolutePath.Create(root);
    }

    private sealed class EnvironmentTestBuild : TestBuild, ITestEnvironmentBuild;

    private sealed class ProvisionedTestBuild : TestBuild, ITestEnvironmentBuild, IFlywayBuild;

    private sealed class StandaloneFlywayTestBuild : TestBuild, IFlywayBuild;

    private class TestBuild : FalloutBuild, IContainerBuild, INuGetBuild, ILayeringBuild,
                              IDependencyMapBuild, IBoilerplateBuild, IDependencyUpgradeBuild, IGitHooksBuild,
                              IDockerBuild, IMockServerBuild, IFalloutBuild
    {
        Verbosity IFalloutBuild.Verbosity => Fallout.Common.Verbosity.Quiet;
        public AbsolutePath TestRoot { get; set; } = AbsolutePath.Create(Directory.GetCurrentDirectory());
        AbsolutePath IFalloutBuild.RootDirectory => TestRoot;

        public Solution Solution { get; set; } = new(new SolutionModel(),
            AbsolutePath.Create(Path.Combine(Directory.GetCurrentDirectory(), "test.sln")));

        public List<Artifact> ArtifactList { get; } = [];
        public IReadOnlyCollection<Artifact> Artifacts => ArtifactList;
        public string ContainerRegistry => "example.invalid";
        public bool IsCiBuild { get; set; }
        public string? HookType { get; set; }
        public bool HookDryRun { get; set; }
        public bool HookSkipLintFix { get; set; }
        public bool HookSkipTestStatic { get; set; }
        public bool HookSkipTestRuntime { get; set; }
        public string? ReleaseProject { get; set; }
        public string? ReleaseBump { get; set; }
        public string? ReleaseBase { get; set; }
        public string? NuGetSource { get; set; }
        public string? BoilerplateItems { get; set; }
        public string? SourceRoot { get; set; }
        public string? TestsRoot { get; set; }
        public string? PreferenceKey { get; set; }
        public string? PreferenceValue { get; set; }
        public string? LayeringModeOverride { get; set; }
        public string[]? LayeringLayers { get; set; }
        public string? LayeringRoot { get; set; }
        public string? LayeringLibraryRoot { get; set; }
        public string? LayeringApplicationFolders { get; set; }
        public string[] DockerWaitFor { get; set; } = [];
        public bool DockerWaitForHealthy { get; set; }
        public int DockerTimeout { get; set; }

        public AbsolutePath DependencyMapOutput { get; set; } =
            AbsolutePath.Create(Path.Combine(Directory.GetCurrentDirectory(), "map.html"));

        public string DependencyVersionLock { get; set; } = "Major";
    }

    private sealed class TemporaryRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fallout-target-" + Guid
            .NewGuid()
            .ToString("N"));

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
            build.Solution = new Solution(new SolutionModel(),
                AbsolutePath.Create(System.IO.Path.Combine(Path, "test.sln")));

            return build;
        }

        public void Dispose() => TestDirectory.Delete(Path);
    }
}
