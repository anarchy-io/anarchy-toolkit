using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Service;

public class LayeringTest
{
    private static readonly SolutionLayout Monorepo = SolutionLayout.Default;

    private static readonly SolutionLayout SingleApplication = new(LayeringMode.Folder, ["Core", "Infrastructure", "Bootstrap", "Runtime", "Apps"]) {
        LibraryRoot = "src",
        ApplicationFolders = new Dictionary<string, string> { ["src/App"] = "Apps" }
    };

    private static readonly SolutionLayout Prefix = new(LayeringMode.Prefix, LayeringTasks.DefaultLayers);

    private static LayeringProject Project(
        string name,
        string[]? references = null,
        Dictionary<string, string>? sources = null,
        string[]? folders = null) => new(
        name,
        name.Split('.')[0],
        "Shop." + name,
        references ?? [],
        sources ?? [],
        folders ?? []);

    private static LayeringProject Located(SolutionLayout layout, string name, string path, params string[] references) =>
        new(name, LayeringTasks.GetLayer(name, path, layout)!, "Shop." + name, references, new Dictionary<string, string>(), []);

    private static IReadOnlyList<string> Check(params LayeringProject[] projects) =>
        LayeringTasks.Check(projects, LayeringTasks.DefaultLayers);

    [Fact]
    public void DefaultLayers_PutAppsOnTop()
    {
        LayeringTasks.DefaultLayers.Should().Equal("Core", "Infrastructure", "Bootstrap", "Runtime", "Apps");
    }

    [Theory]
    [InlineData("Library.Runtime", "Runtime")]
    [InlineData("Library.Core.Events", "Core")]
    [InlineData("Runtime.Web", "Runtime")]
    [InlineData("Apps.Api.Orders", "Apps")]
    [InlineData("Library", "Library")]
    public void PrefixOf_GivenProjectName_SkipsALeadingLibrarySegment(string name, string expected)
    {
        LayeringTasks.PrefixOf(name).Should().Be(expected);
    }

    [Fact]
    public void GetLayer_GivenPrefixLibraryProject_UsesTheLayerAfterLibrary()
    {
        LayeringTasks.GetLayer("Library.Runtime", "src/Library.Runtime/Library.Runtime.csproj", Prefix).Should().Be("Runtime");
        LayeringTasks.GetLayer("Library.Core.Events", "src/Library.Core.Events/Library.Core.Events.csproj", Prefix).Should().Be("Core");
        LayeringTasks.GetPrefixLayer("Library.Infrastructure", "src/Library.Infrastructure/Library.Infrastructure.csproj", "src")
            .Should().Be("Infrastructure");
    }

    [Theory]
    [InlineData("Core.Events", "src/Core.Events/Core.Events.csproj", "Core")]
    [InlineData("Tools.Cli", "src\\Tools.Cli\\Tools.Cli.csproj", "Tools")]
    [InlineData("Unit", "tests/Unit/Unit.csproj", null)]
    [InlineData("_build", "build/_build.csproj", null)]
    public void GetPrefixLayer_GivenProject_ReturnsExpected(string name, string relativePath, string? expected)
    {
        LayeringTasks.GetPrefixLayer(name, relativePath, "src").Should().Be(expected);
    }

    [Theory]
    [InlineData("src/Core/Project1/Project1.csproj", "Core")]
    [InlineData("src\\Infrastructure\\Nested\\Project.csproj", "Infrastructure")]
    [InlineData("src/Testing/Helpers/Helpers.csproj", "Testing")]
    [InlineData("src/Core.Events/Core.Events.csproj", "Core.Events")]
    [InlineData("src/Core.csproj", "")]
    [InlineData("tests/Unit/Unit.csproj", null)]
    [InlineData("src-other/Core/Project.csproj", null)]
    public void GetFolderLayer_GivenProject_ReturnsExpected(string path, string? expected)
    {
        LayeringTasks.GetFolderLayer(path, "src").Should().Be(expected);
    }

    [Theory]
    [InlineData("components/Core/App/App.csproj", "components", "Core")]
    [InlineData("components\\Web\\App.csproj", "./components/", "Web")]
    [InlineData("components-other/Core/App.csproj", "components", null)]
    [InlineData("components/App.csproj", "components", "")]
    [InlineData("Core/App.csproj", ".", "Core")]
    [InlineData("../outside/App.csproj", ".", null)]
    public void GetFolderLayer_GivenCustomRoot_ReturnsExpected(string path, string root, string? expected)
    {
        LayeringTasks.GetFolderLayer(path, root).Should().Be(expected);
    }

    [Theory]
    [InlineData("Orders", "src/Apps/Apis/Orders/Orders.csproj", "Apps")]
    [InlineData("Reports", "src/Apps/Jobs/Reports/Reports.csproj", "Apps")]
    [InlineData("Admin", "src/Apps/Ui/Admin/Admin.csproj", "Apps")]
    [InlineData("Core", "src/Library/Core/Core.csproj", "Core")]
    [InlineData("Testing", "src/Library/Testing/Testing.csproj", "Testing")]
    [InlineData("Apps.Api.Orders", "src/Library/Core/Apps.Api.Orders.csproj", "Core")]
    [InlineData("Tools", "src/Tools/Tools/Tools.csproj", "")]
    [InlineData("Unit", "tests/Unit/Unit.csproj", null)]
    public void GetLayer_GivenMonorepoFolderLayout_UsesApplicationFoldersThenTheLibraryRoot(string name, string path, string? expected)
    {
        LayeringTasks.GetLayer(name, path, Monorepo).Should().Be(expected);
    }

    [Theory]
    [InlineData("App", "src/App/App.csproj", "Apps")]
    [InlineData("Core", "src/Core/Core.csproj", "Core")]
    [InlineData("Bootstrap", "src/Bootstrap/Bootstrap.csproj", "Bootstrap")]
    [InlineData("Loose", "src/Loose.csproj", "")]
    public void GetLayer_GivenSingleApplicationFolderLayout_MapsTheApplicationAndTheLayerProjects(string name, string path, string expected)
    {
        LayeringTasks.GetLayer(name, path, SingleApplication).Should().Be(expected);
    }

    [Fact]
    public void GetLayer_GivenPrefixMode_NeverUsesFolders()
    {
        LayeringTasks.GetLayer("Core.Orders", "src/Api/Orders/Orders.csproj", Prefix).Should().Be("Core");
        LayeringTasks.GetLayer("Orders", "src/Library/Core/Orders.csproj", Prefix).Should().Be("Orders");
        LayeringTasks.GetLayer("Orders", "apps/Orders/Orders.csproj", Prefix).Should().BeNull();
    }

    [Theory]
    [InlineData(SolutionLayoutPreset.Monorepo, LayeringMode.Folder)]
    [InlineData(SolutionLayoutPreset.Monorepo, LayeringMode.Prefix)]
    [InlineData(SolutionLayoutPreset.SingleApp, LayeringMode.Folder)]
    [InlineData(SolutionLayoutPreset.SingleApp, LayeringMode.Prefix)]
    public void Presets_GivenCustomRootAndApplicationFolder_PassTheLayeringCheck(
        SolutionLayoutPreset preset,
        LayeringMode mode)
    {
        var layout = SolutionLayoutPresets.Create(preset, ApplicationKind.Api, "Shop", "code");
        if (mode == LayeringMode.Prefix)
            layout = layout with { Mode = mode, LibraryRoot = null, ApplicationFolders = new Dictionary<string, string>() };

        Func<string, string> libraryPath = mode == LayeringMode.Folder
            ? layout.LibraryProjectPath
            : layer => $"code/{layer}/{layer}.csproj";
        var core = Located(layout, mode == LayeringMode.Folder ? "Core" : "Core", libraryPath("Core"));
        var infrastructure = Located(layout, mode == LayeringMode.Folder ? "Infrastructure" : "Infrastructure", libraryPath("Infrastructure"), core.Name);
        var configuration = Located(layout, "Bootstrap", libraryPath("Bootstrap"), infrastructure.Name);
        var runtime = Located(layout, "Runtime", libraryPath("Runtime"), configuration.Name);
        var appPath = mode == LayeringMode.Folder
            ? $"code/{(preset == SolutionLayoutPreset.SingleApp ? "Shop" : "Apps/Apis")}/Orders/Orders.csproj"
            : "code/Apps.Api.Orders/Apps.Api.Orders.csproj";
        var app = Located(layout, mode == LayeringMode.Folder ? "Orders" : "Apps.Api.Orders", appPath, runtime.Name);

        LayeringTasks.Check([core, infrastructure, configuration, runtime, app], layout).Should().BeEmpty();
        LayeringTasks.GetLayer(app.Name, appPath, layout).Should().Be("Apps");
    }

    [Fact]
    public void Check_GivenFolderLayers_EnforcesAllRules()
    {
        var core = new LayeringProject("Project1", LayeringTasks.GetLayer("Project1", "src/Library/Core/Project1/Project1.csproj", Monorepo)!,
            "Shop.Project1", ["Project2", "Helpers"], new Dictionary<string, string> { ["A.cs"] = "using Shop.Project2;" }, ["Runtime"]);
        var web = new LayeringProject("Project2", LayeringTasks.GetLayer("Project2", "src/Library/Runtime/Project2/Project2.csproj", Monorepo)!,
            "Shop.Project2", ["Project1"], new Dictionary<string, string>(), ["Core"]);
        var testing = new LayeringProject("Helpers", LayeringTasks.GetLayer("Helpers", "src/Library/Testing/Helpers/Helpers.csproj", Monorepo)!,
            "Shop.Helpers", ["Project2"], new Dictionary<string, string>(), ["Runtime"]);

        var result = LayeringTasks.Check([core, web, testing], Monorepo);

        result.Should().Equal(
            "project Project1 (Core) -> Project2 (Runtime)",
            "project Project1 (Core) -> Helpers (Testing)",
            "file Project1/A.cs (Core) uses Shop.Project2 (Runtime)",
            "folder Project1/Runtime/ (Core) is not below the project's layer");
    }

    [Fact]
    public void Check_GivenMonorepoFolderLayout_AllowsDownwardAndRejectsUpwardDependencies()
    {
        var core = Located(Monorepo, "Core", "src/Library/Core/Core.csproj");
        var configuration = Located(Monorepo, "Bootstrap", "src/Library/Bootstrap/Bootstrap.csproj", "Core");
        var runtime = Located(Monorepo, "Runtime", "src/Library/Runtime/Runtime.csproj", "Bootstrap");
        var api = Located(Monorepo, "Orders", "src/Apps/Apis/Orders/Orders.csproj", "Runtime");
        var job = Located(Monorepo, "Reports", "src/Apps/Jobs/Reports/Reports.csproj", "Bootstrap");

        LayeringTasks.Check([core, configuration, runtime, api, job], Monorepo).Should().BeEmpty();

        var upward = Located(Monorepo, "Runtime", "src/Library/Runtime/Runtime.csproj", "Orders");
        LayeringTasks.Check([core, configuration, upward, api, job], Monorepo)
            .Should().Equal("project Runtime (Runtime) -> Orders (Apps)");
    }

    [Fact]
    public void Check_GivenSingleApplicationFolderLayout_AllowsDownwardAndRejectsUpwardDependencies()
    {
        var core = Located(SingleApplication, "Core", "src/Core/Core.csproj");
        var web = Located(SingleApplication, "Runtime", "src/Runtime/Runtime.csproj", "Core");
        var app = Located(SingleApplication, "App", "src/App/App.csproj", "Runtime");

        LayeringTasks.Check([core, web, app], SingleApplication).Should().BeEmpty();

        var upward = Located(SingleApplication, "Core", "src/Core/Core.csproj", "App");
        LayeringTasks.Check([upward, web, app], SingleApplication).Should().Equal("project Core (Core) -> App (Apps)");
    }

    [Fact]
    public void Check_GivenProjectsInSameLayerFolder_AllowsTheirDependencies()
    {
        var first = new LayeringProject("Project1", LayeringTasks.GetFolderLayer("src/Core/Project1/Project1.csproj", "src")!,
            "Shop.Project1", ["Project2"], new Dictionary<string, string> { ["A.cs"] = "using Shop.Project2;" }, []);
        var second = new LayeringProject("Project2", LayeringTasks.GetFolderLayer("src/Core/Project2/Project2.csproj", "src")!,
            "Shop.Project2", [], new Dictionary<string, string>(), []);

        LayeringTasks.Check([first, second], LayeringTasks.DefaultLayers).Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenFolderMode_DoesNotTreatPrefixLikeNamesAsViolations()
    {
        var core = Located(Monorepo, "Apps.Core", "src/Library/Core/Apps.Core.csproj");
        var api = Located(Monorepo, "Core.Api", "src/Apps/Apis/Core.Api/Core.Api.csproj", "Apps.Core");

        LayeringTasks.Check([core, api], Monorepo).Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenMixedConventions_RejectsThemWithoutFallingBackToTheOtherMode()
    {
        // A prefix-named project outside the folder layout is not given its prefix, and a folder-placed project
        // without a layer prefix is not given its folder.
        var prefixInFolderMode = Located(Monorepo, "Web.Ui", "src/Web.Ui/Web.Ui.csproj");
        var folderInPrefixMode = Located(Prefix, "Project1", "src/Core/Project1/Project1.csproj");

        LayeringTasks.Check([prefixInFolderMode], Monorepo)
            .Should().ContainSingle().Which.Should().StartWith("project Web.Ui has no known layer folder").And.Contain("applicationFolders");
        LayeringTasks.Check([folderInPrefixMode], Prefix)
            .Should().ContainSingle().Which.Should().StartWith("project Project1 has no known layer prefix");
    }

    [Fact]
    public void Check_GivenTheToolkitsOwnPrefixLayout_FindsNoViolations()
    {
        var root = RepositoryRoot();
        var layout = LayeringConfiguration.Read(root);
        layout.Mode.Should().Be(LayeringMode.Prefix);
        layout.Layers.Should().Equal("Core", "Infrastructure", "Bootstrap", "Runtime", "Automation");

        var projects = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/") && !path.Contains("/obj/") && !path.Contains("/Boilerplate/"))
            .Select(path => LayeringTasks.GetLayer(Path.GetFileNameWithoutExtension(path), path, layout))
            .ToArray();

        projects.Should().NotBeEmpty().And.OnlyContain(layer => layer == LayeringTasks.Testing || layout.Layers.Contains(layer!));
        LayeringTasks.GetLayer("Automation.Build.Cli", "src/Automation.Build.Cli/Automation.Build.Cli.csproj", layout).Should().Be("Automation");
        LayeringTasks.GetLayer("_build", "build/_build.csproj", layout).Should().BeNull();
    }

    [Fact]
    public void Check_GivenLayeredSolution_ReturnsNoViolations()
    {
        var result = Check(
            Project("Core"),
            Project("Infrastructure.Db", ["Core"], new() { ["Repo.cs"] = "using Shop.Core.Entity;" }, ["Core"]),
            Project("Runtime", ["Infrastructure.Db", "Core"]),
            Project("Testing", ["Runtime"], new() { ["Kit.cs"] = "using Shop.Runtime;" }, ["Runtime"]));

        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenAutomationLayer_EnforcesItsDependencies()
    {
        string[] layers = ["Core", "Infrastructure", "Bootstrap", "Runtime", "Automation"];
        var automation = Project("Automation", ["Runtime"]);
        var tool = Project("Automation.Build.Cli", ["Automation"]);
        var runtime = Project("Runtime", ["Automation"]);

        LayeringTasks.Check([automation, tool, runtime], layers).Should().Equal("project Runtime (Runtime) -> Automation (Automation)");
    }

    [Fact]
    public void Check_GivenUnknownLayer_ReportsProject()
    {
        var result = Check(Project("Tools"));

        result.Should().Equal("project Tools has no known layer prefix (Core, Infrastructure, Bootstrap, Runtime, Apps, Testing)");
    }

    [Fact]
    public void Check_GivenUpwardOrTestKitReference_ReportsBoth()
    {
        var result = Check(Project("Core", ["Runtime", "Testing"]), Project("Runtime"), Project("Testing"));

        result.Should().Equal("project Core (Core) -> Runtime (Runtime)", "project Core (Core) -> Testing (Testing)");
    }

    [Theory]
    [InlineData("using Shop.Runtime.Web.Ui.Page;")]
    [InlineData("global using Shop.Runtime.Web.Ui;")]
    [InlineData("  using static Shop.Runtime.Web.Ui.Helpers;")]
    [InlineData("using Page = Shop.Runtime.Web.Ui.Page;")]
    public void Check_GivenUpwardImport_ReportsFile(string line)
    {
        var core = Project("Core", sources: new() { ["Entity/Order.cs"] = $"namespace Shop.Core;\n{line}\n" });

        var result = Check(core, Project("Runtime"), Project("Runtime.Web.Ui"));

        result.Should().ContainSingle().Which.Should().StartWith("file Core/Entity/Order.cs (Core) uses Shop.Runtime.Web.Ui");
    }

    [Fact]
    public void Check_GivenNamespaceSharingAPrefix_UsesTheMostSpecificOwner()
    {
        // Shop.Core.Events is owned by Core.Events, not by Core, and Shop.CoreX by nobody.
        var infrastructure = Project("Infrastructure.Db", sources: new() { ["A.cs"] = "using Shop.Core.Events;\nusing Shop.CoreX;" });

        var result = Check(Project("Core"), Project("Core.Events"), infrastructure);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenOwnOrHigherLayerFolder_ReportsFolders()
    {
        var result = Check(Project("Infrastructure.Db", folders: ["Core", "Infrastructure", "Runtime", "Service"]));

        result.Should().Equal(
            "folder Infrastructure.Db/Infrastructure/ (Infrastructure) is not below the project's layer",
            "folder Infrastructure.Db/Runtime/ (Infrastructure) is not below the project's layer");
    }

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "..", ".."));
}
