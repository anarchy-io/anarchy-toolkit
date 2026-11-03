using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using AwesomeAssertions;
using Nuke.Common.IO;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class DependencyMapTest
{
    private static readonly string[] Layers = ["Core", "Infrastructure", "Configuration", "Web"];

    private static DependencyMap Map(params DependencyMapReference[] references) => new(
        "Shop",
        Layers,
        [
            new DependencyMapProject("Core", "Core", 10, 400),
            new DependencyMapProject("Infrastructure.Db", "Infrastructure", 5, 200),
            new DependencyMapProject("Web", "Web", 4, 100),
            new DependencyMapProject("Testing", "Testing", 2, 50),
            new DependencyMapProject("Unit", "Tests", 8, 250),
        ],
        references);

    [Theory]
    [InlineData("Core.Events", "src/Core.Events/Core.Events.csproj", "Core")]
    [InlineData("Web.Ui", "src/Web.Ui/Web.Ui.csproj", "Web")]
    [InlineData("Testing.Db", "src/Testing.Db/Testing.Db.csproj", "Testing")]
    [InlineData("Unit", "tests/Unit/Unit.csproj", "Tests")]
    [InlineData("Samples.Api", "samples/Samples.Api/Samples.Api.csproj", "Samples")]
    [InlineData("Tools.Cli", "src/Tools.Cli/Tools.Cli.csproj", "Other")]
    [InlineData("_build", "build/_build.csproj", null)]
    public void GetDefaultGroup_GivenProject_ReturnsExpected(string name, string relativePath, string? expected)
    {
        // Act
        var result = DependencyMapTasks.GetDefaultGroup(name, relativePath, Layers);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Web", "Core", false)]
    [InlineData("Infrastructure.Db", "Infrastructure.Db", false)]
    [InlineData("Core", "Web", true)]
    [InlineData("Infrastructure.Db", "Testing", true)]
    [InlineData("Unit", "Web", false)]
    [InlineData("Testing", "Infrastructure.Db", false)]
    public void IsViolation_GivenReference_ReturnsExpected(string from, string to, bool expected)
    {
        // Arrange
        var reference = new DependencyMapReference(from, to);

        // Act
        var result = Map(reference).IsViolation(reference);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Groups_ListsLayersInnerToOuterThenOutsideGroupsAlphabetically()
    {
        // Act
        var result = Map().Groups;

        // Assert
        result.Should().Equal("Core", "Infrastructure", "Configuration", "Web", "Testing", "Tests");
    }

    [Fact]
    public void RenderHtml_GivenSameMapInAnyOrder_RendersTheSameBytes()
    {
        // Arrange
        var a = new DependencyMapReference("Web", "Core");
        var b = new DependencyMapReference("Infrastructure.Db", "Core");
        var map = Map(a, b);
        var shuffled = map with { Projects = map.Projects.Reverse().ToArray(), References = [b, a] };

        // Act
        var result = DependencyMapTasks.RenderHtml(map);

        // Assert
        result.Should().Be(DependencyMapTasks.RenderHtml(shuffled));
    }

    [Fact]
    public void RenderHtml_GivenViolation_ListsAndHighlightsIt()
    {
        // Act
        var result = DependencyMapTasks.RenderHtml(Map(new DependencyMapReference("Web", "Core"), new DependencyMapReference("Core", "Web")));

        // Assert
        result.Should().Contain("<strong>1</strong> layering violations")
            .And.Contain("<code>Core</code> → <code>Web</code>")
            .And.Contain("p0 -.-&gt; p2")
            .And.Contain("linkStyle 0 stroke:#c0302f");
    }

    [Fact]
    public void RenderHtml_RendersAClickableRingAndAZoomedInViewPerLayer()
    {
        // Act
        var result = DependencyMapTasks.RenderHtml(Map(new DependencyMapReference("Web", "Core")));

        // Assert
        result.Should().Contain("""<g class="ring" data-group="Core" data-zoom="4" role="button" tabindex="0" """)
            .And.Contain("""<template data-group="Core">""")
            .And.Contain("""<template data-group="Configuration"><p class="muted">No projects.</p></template>""")
            .And.Contain("""<button type="button" data-group="Testing">Testing (1)</button>""");
    }

    [Fact]
    public void WithoutTransitive_GivenImpliedReference_DropsIt()
    {
        // Arrange
        var webToInfrastructure = new DependencyMapReference("Web", "Infrastructure.Db");
        var infrastructureToCore = new DependencyMapReference("Infrastructure.Db", "Core");
        var webToCore = new DependencyMapReference("Web", "Core");

        // Act
        var result = DependencyMapTasks.WithoutTransitive([webToInfrastructure, infrastructureToCore, webToCore]);

        // Assert
        result.Should().BeEquivalentTo([webToInfrastructure, infrastructureToCore]);
    }

    [Fact]
    public void CountSource_IgnoresBuildOutputAndBlankLines()
    {
        // Arrange
        var root = AbsolutePath.Create(Path.GetTempPath()) / Guid.NewGuid().ToString("N");
        (root / "Service" / "A.cs").WriteAllText("class A\n{\n\n}\n");
        (root / "obj" / "B.cs").WriteAllText("class B { }\n");
        (root / "bin" / "C.cs").WriteAllText("class C { }\n");

        try
        {
            // Act
            var result = DependencyMapTasks.CountSource(root);

            // Assert
            result.Should().Be((1, 3));
        }
        finally
        {
            root.DeleteDirectory();
        }
    }
}
