using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;
using global::Build;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Component;

public class PackageHierarchyTest
{
    private static readonly IReadOnlyList<string> Layers = LayeringTasks.DefaultLayers;

    private static LayeringProject Project(string name, params string[] references) =>
        new(name, name.Split('.')[0], "Anarchy.Toolkit." + name, references, new Dictionary<string, string>(), []);

    private static IReadOnlyList<string> Check(IReadOnlyCollection<AllowedPackageReference> allowed, params LayeringProject[] projects) =>
        PackageHierarchy.Check(projects, Layers, allowed);

    [Theory]
    [InlineData("Web", "Web.MockServer.Ui", true)]
    [InlineData("Web.MockServer", "Web.MockServer.Ui", true)]
    [InlineData("Web.Ui", "Web.MockServer.Ui", false)]
    [InlineData("Web.MockServer.Ui", "Web.MockServer.Ui", false)]
    [InlineData("Web.MockServer.Ui", "Web", false)]
    [InlineData("Core", "CoreX.Events", false)]
    public void IsAncestor_GivenPackages_ReturnsExpected(string ancestor, string package, bool expected)
    {
        PackageHierarchy.IsAncestor(ancestor, package).Should().Be(expected);
    }

    [Fact]
    public void Check_GivenSameLayerAncestorReferences_ReturnsNoViolations()
    {
        var result = Check(
            [],
            Project("Web"),
            Project("Web.MockServer", "Web"),
            Project("Web.MockServer.Ui", "Web.MockServer", "Web"),
            Project("Testing"),
            Project("Testing.Db", "Testing"),
            Project("Testing.Db.Mysql", "Testing", "Testing.Db"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenLowerLayerReferenceAcrossFamilies_ReturnsNoViolations()
    {
        var result = Check(
            [],
            Project("Core.Events"),
            Project("Core.Health"),
            Project("Infrastructure.Persistence.Db", "Core.Events", "Core.Health"),
            Project("Testing.Db", "Infrastructure.Persistence.Db"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenSameLayerNonAncestorReferences_ReportsThem()
    {
        var result = Check(
            [],
            Project("Web"),
            Project("Web.Ui", "Web"),
            Project("Web.MockServer.Ui", "Web.Ui"),
            Project("Web.Api", "Web.Ui"));

        result.Should().HaveCount(2);
        result[0].Should().StartWith("project Web.Api (Web) -> Web.Ui (Web) is outside its package-name ancestors");
        result[1].Should().StartWith("project Web.MockServer.Ui (Web) -> Web.Ui (Web) is outside its package-name ancestors");
    }

    [Fact]
    public void Check_GivenReferenceToADescendant_ReportsIt()
    {
        var result = Check([], Project("Web", "Web.Ui"), Project("Web.Ui"));

        result.Should().ContainSingle().Which.Should().StartWith("project Web (Web) -> Web.Ui (Web)");
    }

    [Fact]
    public void Check_GivenAllowedReference_ReturnsNoViolations()
    {
        var result = Check(
            [new AllowedPackageReference("Web.MockServer.Ui", "Web.Ui", "shared UI shell")],
            Project("Web.Ui"),
            Project("Web.MockServer.Ui", "Web.Ui"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_GivenAllowedReferenceInTheOtherDirection_StillReportsTheReference()
    {
        var result = Check(
            [new AllowedPackageReference("Web.Ui", "Web.MockServer.Ui", "shared UI shell")],
            Project("Web.Ui"),
            Project("Web.MockServer.Ui", "Web.Ui"));

        result.Should().HaveCount(2);
        result[0].Should().StartWith("project Web.MockServer.Ui (Web) -> Web.Ui (Web)");
        result[1].Should().StartWith("allowed reference Web.Ui -> Web.MockServer.Ui is stale");
    }

    [Fact]
    public void Check_GivenHigherLayerReference_LeavesItToTheLayeringCheck()
    {
        LayeringProject[] projects = [Project("Core.Events", "Web.Ui"), Project("Web.Ui")];

        PackageHierarchy.Check(projects, Layers, []).Should().BeEmpty();
        LayeringTasks.Check(projects, Layers).Should().Equal("project Core.Events (Core) -> Web.Ui (Web)");
    }

    [Fact]
    public void Check_GivenProjectsWithoutAKnownLayer_IgnoresThem()
    {
        var result = Check([], Project("Unit", "Unit.Helpers"), Project("Unit.Helpers"), Project("Samples.Api", "Samples.Shared"), Project("Samples.Shared"));

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Web.MockServer.Ui", "Web.Ui")] // no such reference
    [InlineData("Web.MockServer.Ui", "Web")] // an ancestor needs no entry
    [InlineData("Web.MockServer.Ui", "Core")] // nor does a lower layer
    public void Check_GivenAllowedReferenceThatMatchesNothing_ReportsItAsStale(string from, string to)
    {
        var result = Check(
            [new AllowedPackageReference(from, to, "obsolete")],
            Project("Core"),
            Project("Web"),
            Project("Web.Ui", "Web"),
            Project("Web.MockServer.Ui", "Web", "Core"));

        result.Should().Equal(
            $"allowed reference {from} -> {to} is stale: no same-layer reference outside the package-name ancestors matches it; remove it");
    }

    [Fact]
    public void Check_GivenAllowedReferenceWithoutAReason_ReportsIt()
    {
        var result = Check(
            [new AllowedPackageReference("Web.MockServer.Ui", "Web.Ui", " ")],
            Project("Web.Ui"),
            Project("Web.MockServer.Ui", "Web.Ui"));

        result.Should().Equal("allowed reference Web.MockServer.Ui -> Web.Ui has no reason");
    }

    [Fact]
    public void AllowedReferences_AreUniqueWithAReason()
    {
        PackageHierarchy.AllowedReferences.Select(a => (a.From, a.To)).Should().OnlyHaveUniqueItems();
        PackageHierarchy.AllowedReferences.Should().OnlyContain(a => !string.IsNullOrWhiteSpace(a.Reason));
    }

    [Fact]
    public void Check_GivenTheToolkitsOwnProjects_FindsNoViolations()
    {
        var root = RepositoryRoot();
        var layout = LayeringConfiguration.Read(root);

        var projects = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/") && !path.Contains("/obj/") && !path.Contains("/Boilerplate/"))
            .Select(path => (Name: Path.GetFileNameWithoutExtension(path), Path: path))
            .Select(x => (x.Name, x.Path, Layer: LayeringTasks.GetLayer(x.Name, x.Path, layout)))
            .Where(x => x.Layer is not null)
            .Select(x => new LayeringProject(
                x.Name,
                x.Layer!,
                "Anarchy.Toolkit." + x.Name,
                ProjectReferences(Path.Combine(root, x.Path)),
                new Dictionary<string, string>(),
                []))
            .ToArray();

        projects.Should().Contain(p => p.Name == "Web.MockServer.Ui");
        LayeringTasks.Check(projects, layout).Should().BeEmpty();
        PackageHierarchy.Check(projects, layout.Layers, PackageHierarchy.AllowedReferences).Should().BeEmpty();
    }

    private static string[] ProjectReferences(string projectFile) =>
        XDocument.Load(projectFile)
            .Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .OfType<string>()
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .ToArray();

    private static string RepositoryRoot([CallerFilePath] string path = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Anarchy.Toolkit.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find Anarchy.Toolkit.sln.");
    }
}
