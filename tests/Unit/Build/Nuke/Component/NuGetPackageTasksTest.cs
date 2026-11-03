using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using AwesomeAssertions;
using System.IO.Compression;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class NuGetPackageTasksTest
{
    [Fact]
    public void OrderForPublish_OrdersTransitiveAndSameLayerDependenciesBeforeConsumers()
    {
        using var packages = new PackageFiles();
        var web = packages.Add("Web", "1.0.0", ("Infrastructure", "[1.0.0, )"));
        var infra = packages.Add("Infrastructure", "1.0.0", ("Core.Events", "[1.0.0, )"));
        var events = packages.Add("Core.Events", "1.0.0", ("Core", "[1.0.0, )"));
        var core = packages.Add("Core", "1.0.0", ("External", "[1.0.0, )"));
        var testing = packages.Add("Testing", "1.0.0", ("Web", "[1.0.0, )"));

        NuGetPackageTasks.OrderForPublish([testing, web, infra, events, core])
            .Should().Equal(core, events, infra, web, testing);
    }

    [Fact]
    public void OrderForPublish_RejectsCyclesBeforePublishing()
    {
        using var packages = new PackageFiles();
        var a = packages.Add("A", "1.0.0", ("B", "[1.0.0, )"));
        var b = packages.Add("B", "1.0.0", ("A", "[1.0.0, )"));

        var act = () => NuGetPackageTasks.OrderForPublish([a, b]);
        act.Should().Throw<InvalidOperationException>().WithMessage("*cycle*");
    }

    [Fact]
    public void OrderForPublish_ConsidersDependenciesFromAllFrameworks()
    {
        using var packages = new PackageFiles();
        var a = packages.AddWithGroups("A", "1.0.0", """
            <group targetFramework="net8.0"><dependency id="B" version="[1.0.0, )" /></group>
            <group targetFramework="net10.0"><dependency id="C" version="[1.0.0, )" /></group>
            """);
        var b = packages.Add("B", "1.0.0");
        var c = packages.Add("C", "1.0.0");

        NuGetPackageTasks.OrderForPublish([a, b, c]).Should().Equal(b, c, a);
    }

    [Fact]
    public void OrderForPublish_RejectsIncompatibleVersionAndDuplicateId()
    {
        using var packages = new PackageFiles();
        var a = packages.Add("A", "1.0.0", ("B", "[2.0.0, )"));
        var b = packages.Add("B", "1.0.0");
        var otherB = packages.Add("b", "2.0.0");

        var incompatible = () => NuGetPackageTasks.OrderForPublish([a, b]);
        incompatible.Should().Throw<InvalidOperationException>().WithMessage("*requires B*");
        var duplicate = () => NuGetPackageTasks.OrderForPublish([b, otherB]);
        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*Multiple packages*");
    }

    [Fact]
    public void FindPackages_ExcludesLegacySymbols()
    {
        using var packages = new PackageFiles();
        var primary = packages.Add("A", "1.0.0");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(primary)!, "A.1.0.0.symbols.nupkg"), "symbols");

        NuGetPackageTasks.FindPackages(Path.GetDirectoryName(primary)!).Should().Equal(primary);
    }

    private sealed class PackageFiles : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public PackageFiles() => Directory.CreateDirectory(_directory);

        public string Add(string id, string version, params (string Id, string Version)[] dependencies)
        {
            var items = string.Concat(dependencies.Select(d => $"<dependency id=\"{d.Id}\" version=\"{d.Version}\" />"));
            return AddWithGroups(id, version, $"<group targetFramework=\"net10.0\">{items}</group>");
        }

        public string AddWithGroups(string id, string version, string groups)
        {
            var path = Path.Combine(_directory, $"{id}.{version}.nupkg");
            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            using var writer = new StreamWriter(archive.CreateEntry($"{id}.nuspec").Open());
            writer.Write($"<package><metadata><id>{id}</id><version>{version}</version><authors>Test</authors><description>Test</description><dependencies>{groups}</dependencies></metadata></package>");
            return path;
        }

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }
}
