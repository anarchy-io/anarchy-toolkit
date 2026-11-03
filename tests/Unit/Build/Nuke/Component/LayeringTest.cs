using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class LayeringTest
{
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

    private static IReadOnlyList<string> Check(params LayeringProject[] projects) =>
        LayeringTasks.Check(projects, LayeringTasks.DefaultLayers);

    [Theory]
    [InlineData("Core.Events", "src/Core.Events/Core.Events.csproj", "Core")]
    [InlineData("Tools.Cli", "src\\Tools.Cli\\Tools.Cli.csproj", "Tools")]
    [InlineData("Unit", "tests/Unit/Unit.csproj", null)]
    [InlineData("_build", "build/_build.csproj", null)]
    public void GetDefaultLayer_GivenProject_ReturnsExpected(string name, string relativePath, string? expected)
    {
        // Act
        var result = LayeringTasks.GetDefaultLayer(name, relativePath);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("src/Core/Project1/Project1.csproj", "Core")]
    [InlineData("src/Core/Project2/Project2.csproj", "Core")]
    [InlineData("src\\Infrastructure\\Nested\\Project.csproj", "Infrastructure")]
    [InlineData("src/Testing/Helpers/Helpers.csproj", "Testing")]
    [InlineData("src/Core.Events/Core.Events.csproj", "Core.Events")]
    [InlineData("src/Core.csproj", "")]
    [InlineData("tests/Unit/Unit.csproj", null)]
    [InlineData("src-other/Core/Project.csproj", null)]
    public void GetFolderLayer_GivenProject_ReturnsExpected(string path, string? expected)
    {
        LayeringTasks.GetFolderLayer(path).Should().Be(expected);
    }

    [Fact]
    public void Check_GivenFolderLayers_EnforcesAllRules()
    {
        var core = new LayeringProject("Project1", LayeringTasks.GetFolderLayer("src/Core/Project1/Project1.csproj")!,
            "Shop.Project1", ["Project2", "Helpers"], new Dictionary<string, string> { ["A.cs"] = "using Shop.Project2;" }, ["Web"]);
        var web = new LayeringProject("Project2", LayeringTasks.GetFolderLayer("src/Web/Project2/Project2.csproj")!,
            "Shop.Project2", ["Project1"], new Dictionary<string, string>(), ["Core"]);
        var testing = new LayeringProject("Helpers", LayeringTasks.GetFolderLayer("src/Testing/Helpers/Helpers.csproj")!,
            "Shop.Helpers", ["Project2"], new Dictionary<string, string>(), ["Web"]);

        var result = LayeringTasks.Check([core, web, testing], LayeringTasks.DefaultLayers, true);

        result.Should().Equal(
            "project Project1 (Core) -> Project2 (Web)",
            "project Project1 (Core) -> Helpers (Testing)",
            "file Project1/A.cs (Core) uses Shop.Project2 (Web)",
            "folder Project1/Web/ (Core) is not below the project's layer");
    }

    [Fact]
    public void Check_GivenProjectsInSameLayerFolder_AllowsTheirDependencies()
    {
        var first = new LayeringProject("Project1", LayeringTasks.GetFolderLayer("src/Core/Project1/Project1.csproj")!,
            "Shop.Project1", ["Project2"], new Dictionary<string, string> { ["A.cs"] = "using Shop.Project2;" }, []);
        var second = new LayeringProject("Project2", LayeringTasks.GetFolderLayer("src/Core/Project2/Project2.csproj")!,
            "Shop.Project2", [], new Dictionary<string, string>(), []);

        LayeringTasks.Check([first, second], LayeringTasks.DefaultLayers, true).Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenMixedConventions_DoesNotFallBackToOtherConvention()
    {
        var folderProject = Project("Project1") with { Layer = LayeringTasks.GetDefaultLayer("Project1", "src/Core/Project1/Project1.csproj")! };
        var prefixProject = Project("Web.Ui") with { Layer = LayeringTasks.GetFolderLayer("src/Web.Ui/Web.Ui.csproj")! };

        Check(folderProject).Should().ContainSingle().Which.Should().Contain("no known layer prefix");
        LayeringTasks.Check([prefixProject], LayeringTasks.DefaultLayers, true)
            .Should().ContainSingle().Which.Should().Contain("no known layer folder");
    }

    [Fact]
    public void Check_GivenLayeredSolution_ReturnsNoViolations()
    {
        // Act
        var result = Check(
            Project("Core"),
            Project("Infrastructure.Db", ["Core"], new() { ["Repo.cs"] = "using Shop.Core.Entity;" }, ["Core"]),
            Project("Web", ["Infrastructure.Db", "Core"]),
            Project("Testing", ["Web"], new() { ["Kit.cs"] = "using Shop.Web;" }, ["Web"]));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenUnknownLayer_ReportsProject()
    {
        // Act
        var result = Check(Project("Tools"));

        // Assert
        result.Should().Equal("project Tools has no known layer prefix (Core, Infrastructure, Configuration, Hosting, Web, Build, Testing)");
    }

    [Fact]
    public void Check_GivenUpwardOrTestKitReference_ReportsBoth()
    {
        // Act
        var result = Check(Project("Core", ["Web", "Testing"]), Project("Web"), Project("Testing"));

        // Assert
        result.Should().Equal("project Core (Core) -> Web (Web)", "project Core (Core) -> Testing (Testing)");
    }

    [Theory]
    [InlineData("using Shop.Web.Ui.Page;")]
    [InlineData("global using Shop.Web.Ui;")]
    [InlineData("  using static Shop.Web.Ui.Helpers;")]
    [InlineData("using Page = Shop.Web.Ui.Page;")]
    public void Check_GivenUpwardImport_ReportsFile(string line)
    {
        // Arrange
        var core = Project("Core", sources: new() { ["Entity/Order.cs"] = $"namespace Shop.Core;\n{line}\n" });

        // Act
        var result = Check(core, Project("Web"), Project("Web.Ui"));

        // Assert
        result.Should().ContainSingle().Which.Should().StartWith("file Core/Entity/Order.cs (Core) uses Shop.Web.Ui");
    }

    [Fact]
    public void Check_GivenNamespaceSharingAPrefix_UsesTheMostSpecificOwner()
    {
        // Arrange: Shop.Core.Events is owned by Core.Events, not by Core, and Shop.CoreX by nobody.
        var infrastructure = Project("Infrastructure.Db", sources: new() { ["A.cs"] = "using Shop.Core.Events;\nusing Shop.CoreX;" });

        // Act
        var result = Check(Project("Core"), Project("Core.Events"), infrastructure);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenOwnOrHigherLayerFolder_ReportsFolders()
    {
        // Act
        var result = Check(Project("Infrastructure.Db", folders: ["Core", "Infrastructure", "Web", "Service"]));

        // Assert
        result.Should().Equal(
            "folder Infrastructure.Db/Infrastructure/ (Infrastructure) is not below the project's layer",
            "folder Infrastructure.Db/Web/ (Infrastructure) is not below the project's layer");
    }
}
