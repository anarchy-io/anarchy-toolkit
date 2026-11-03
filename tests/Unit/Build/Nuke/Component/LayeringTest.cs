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
