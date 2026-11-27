using Anarchy.Toolkit.Automation.Helpers.Solutions;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Helpers.Solutions;

public class SolutionItemsTest
{
    private const string Solution = """
        Microsoft Visual Studio Solution File, Format Version 12.00
        Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "_build", "build\_build.csproj", "{11111111-1111-1111-1111-111111111111}"
        EndProject
        Global
        	GlobalSection(SolutionConfigurationPlatforms) = preSolution
        	EndGlobalSection
        EndGlobal

        """;

    [Fact]
    public void Add_GivenFilesAndDirectories_ListsThemInNestedSolutionFolders()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(root, "My.sln"), Solution);
            File.WriteAllText(Path.Combine(root, "Makefile"), "");
            Directory.CreateDirectory(Path.Combine(root, "scripts", "make"));
            File.WriteAllText(Path.Combine(root, "scripts", "make", "help.sh"), "");

            SolutionItems.Add(root, "My.sln", ["scripts", "Makefile", "global.json"]);
            SolutionItems.Add(root, "My.sln", ["Makefile"]);

            var solution = File.ReadAllText(Path.Combine(root, "My.sln"));
            solution.Should().Contain("= \"solution items\", \"solution items\"").And.Contain("= \"scripts\", \"scripts\"")
                .And.Contain("= \"make\", \"make\"").And.Contain(@"scripts\make\help.sh = scripts\make\help.sh")
                .And.Contain("GlobalSection(NestedProjects) = preSolution").And.NotContain("global.json");
            solution.Split("Makefile = Makefile").Should().HaveCount(2);
            solution.Split("\"solution items\", \"solution items\"").Should().HaveCount(2);
            solution.IndexOf("EndProject\nGlobal", StringComparison.Ordinal).Should().BePositive();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
