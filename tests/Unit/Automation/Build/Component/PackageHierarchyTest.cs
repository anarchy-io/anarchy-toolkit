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

    private static IReadOnlyList<string> Check(IReadOnlyCollection<AllowedPackageReference> allowed,
                                               params LayeringProject[] projects) =>
        PackageHierarchy.Check(projects, Layers, allowed);

    [Theory]
    [InlineData("Runtime", "Runtime.Web.MockServer.Ui", true)]
    [InlineData("Runtime.Web.MockServer", "Runtime.Web.MockServer.Ui", true)]
    [InlineData("Runtime.Web.Ui", "Runtime.Web.MockServer.Ui", false)]
    [InlineData("Runtime.Web.MockServer.Ui", "Runtime.Web.MockServer.Ui", false)]
    [InlineData("Runtime.Web.MockServer.Ui", "Runtime.Web", false)]
    [InlineData("Core", "CoreX.Events", false)]
    public void IsAncestor_GivenPackages_ReturnsExpected(string ancestor, string package, bool expected)
    {
        PackageHierarchy.IsAncestor(ancestor, package)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void Check_GivenSameLayerAncestorReferences_ReturnsNoViolations()
    {
        var result = Check(
            [],
            Project("Runtime.Web"),
            Project("Runtime.Web.MockServer", "Runtime.Web"),
            Project("Runtime.Web.MockServer.Ui", "Runtime.Web.MockServer", "Runtime.Web"),
            Project("Testing"),
            Project("Testing.Db", "Testing"),
            Project("Testing.Db.Mysql", "Testing", "Testing.Db"));

        result.Should()
            .BeEmpty();
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

        result.Should()
            .BeEmpty();
    }

    [Fact]
    public void Check_GivenSameLayerNonAncestorReferences_ReportsThem()
    {
        var result = Check(
            [],
            Project("Runtime.Web"),
            Project("Runtime.Web.Ui", "Runtime.Web"),
            Project("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui"),
            Project("Runtime.Web.Api", "Runtime.Web.Ui"));

        result.Should()
            .HaveCount(2);

        result[0]
            .Should()
            .StartWith("project Runtime.Web.Api (Runtime) -> Runtime.Web.Ui (Runtime) is outside its package-name ancestors");

        result[1]
            .Should()
            .StartWith("project Runtime.Web.MockServer.Ui (Runtime) -> Runtime.Web.Ui (Runtime) is outside its package-name ancestors");
    }

    [Fact]
    public void Check_GivenReferenceToADescendant_ReportsIt()
    {
        var result = Check([], Project("Runtime.Web", "Runtime.Web.Ui"), Project("Runtime.Web.Ui"));

        result.Should()
            .ContainSingle()
            .Which.Should()
            .StartWith("project Runtime.Web (Runtime) -> Runtime.Web.Ui (Runtime)");
    }

    [Fact]
    public void Check_GivenAllowedReference_ReturnsNoViolations()
    {
        var result = Check(
            [new AllowedPackageReference("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui", "shared UI shell")],
            Project("Runtime.Web.Ui"),
            Project("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui"));

        result.Should()
            .BeEmpty();
    }

    [Fact]
    public void Check_GivenAllowedReferenceInTheOtherDirection_StillReportsTheReference()
    {
        var result = Check(
            [new AllowedPackageReference("Runtime.Web.Ui", "Runtime.Web.MockServer.Ui", "shared UI shell")],
            Project("Runtime.Web.Ui"),
            Project("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui"));

        result.Should()
            .HaveCount(2);

        result[0]
            .Should()
            .StartWith("project Runtime.Web.MockServer.Ui (Runtime) -> Runtime.Web.Ui (Runtime)");

        result[1]
            .Should()
            .StartWith("allowed reference Runtime.Web.Ui -> Runtime.Web.MockServer.Ui is stale");
    }

    [Fact]
    public void Check_GivenHigherLayerReference_LeavesItToTheLayeringCheck()
    {
        LayeringProject[] projects = [Project("Core.Events", "Runtime.Web.Ui"), Project("Runtime.Web.Ui")];

        PackageHierarchy.Check(projects, Layers, [])
            .Should()
            .BeEmpty();

        LayeringTasks.Check(projects, Layers)
            .Should()
            .Equal("project Core.Events (Core) -> Runtime.Web.Ui (Runtime)");
    }

    [Fact]
    public void Check_GivenProjectsWithoutAKnownLayer_IgnoresThem()
    {
        var result = Check([], Project("Unit", "Unit.Helpers"), Project("Unit.Helpers"),
            Project("Samples.Api", "Samples.Shared"), Project("Samples.Shared"));

        result.Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui")] // no such reference
    [InlineData("Runtime.Web.MockServer.Ui", "Runtime.Web")] // an ancestor needs no entry
    [InlineData("Runtime.Web.MockServer.Ui", "Core")] // nor does a lower layer
    public void Check_GivenAllowedReferenceThatMatchesNothing_ReportsItAsStale(string from, string to)
    {
        var result = Check(
            [new AllowedPackageReference(from, to, "obsolete")],
            Project("Core"),
            Project("Runtime.Web"),
            Project("Runtime.Web.Ui", "Runtime.Web"),
            Project("Runtime.Web.MockServer.Ui", "Runtime.Web", "Core"));

        result.Should()
            .Equal(
                $"allowed reference {from} -> {to} is stale: no same-layer reference outside the package-name ancestors matches it; remove it");
    }

    [Fact]
    public void Check_GivenAllowedReferenceWithoutAReason_ReportsIt()
    {
        var result = Check(
            [new AllowedPackageReference("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui", " ")],
            Project("Runtime.Web.Ui"),
            Project("Runtime.Web.MockServer.Ui", "Runtime.Web.Ui"));

        result.Should()
            .Equal("allowed reference Runtime.Web.MockServer.Ui -> Runtime.Web.Ui has no reason");
    }

    [Fact]
    public void AllowedReferences_AreUniqueWithAReason()
    {
        PackageHierarchy.AllowedReferences.Select(a => (a.From, a.To))
            .Should()
            .OnlyHaveUniqueItems();

        PackageHierarchy.AllowedReferences.Should()
            .OnlyContain(a => !string.IsNullOrWhiteSpace(a.Reason));
    }

    [Fact]
    public void Check_GivenTheToolkitsOwnProjects_FindsNoViolations()
    {
        var root = RepositoryRoot();
        var layout = LayeringConfiguration.Read(root);

        var projects = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path)
                .Replace('\\', '/'))
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

        projects.Should()
            .Contain(p => p.Name == "Runtime.Web.MockServer.Ui");

        LayeringTasks.Check(projects, layout)
            .Should()
            .BeEmpty();

        PackageHierarchy.Check(projects, layout.Layers, PackageHierarchy.AllowedReferences)
            .Should()
            .BeEmpty();
    }

    private static string[] ProjectReferences(string projectFile) =>
        XDocument.Load(projectFile)
            .Descendants("ProjectReference")
            .Where(reference => !string.Equals((string?)reference.Attribute("ReferenceOutputAssembly"), "false", StringComparison.OrdinalIgnoreCase))
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
