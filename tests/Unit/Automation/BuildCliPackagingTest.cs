using AwesomeAssertions;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Automation;

public class BuildCliPackagingTest
{
    [Fact]
    public void PackedBoilerplate_KeepsTheSourceLayout()
    {
        var project = XDocument.Load(Path.Combine(RepositoryRoot(), "src", "Automation", "Automation.csproj"));
        var packed = project.Descendants("None")
            .Where(item => (string?)item.Attribute("Pack") == "true")
            .ToArray();

        packed.Should().NotBeEmpty();
        foreach (var item in packed)
        {
            var packagePath = (string?)item.Attribute("PackagePath") ?? "";
            if (packagePath.EndsWith('/'))
                packagePath.Should().NotContain("RecursiveDir", "NuGet already appends %(RecursiveDir) to a folder path");
        }
    }

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", ".."));
}
