using Anarchy.Toolkit.Build.Nuke.Component;
using AwesomeAssertions;
using Nuke.Common.IO;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Component;

public class StandardBuildTest
{
    [Theory]
    [InlineData("Unit", "Unit", true)]
    [InlineData("Tests.Unit.Core", "Unit", true)]
    [InlineData("MyService.Unit.Tests", "Unit", true)]
    [InlineData("MyService.Tests.Unit", "unit", true)]
    [InlineData("Tests.Integration.Web", "Unit", false)]
    [InlineData("Unit.Conversions", "Unit", false)]
    [InlineData("Community.Tests", "Unit", false)]
    [InlineData("Acceptance", "Acceptance", true)]
    public void MatchesTestKind_GivenProjectName_ReturnsExpected(string projectName, string kind, bool expected)
    {
        // Act
        var result = IStandardBuild.MatchesTestKind(projectName, kind);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ParseAppNames_GivenCommaSeparatedNames_TrimsAndDeduplicates()
    {
        // Act
        var result = IStandardBuild.ParseAppNames(" Orders, Payments,,orders ");

        // Assert
        result.Should().Equal("Orders", "Payments");
    }

    [Theory]
    [InlineData("src", "Exe", true)]
    [InlineData("src", "Library", false)]
    [InlineData("tests", "Exe", false)]
    public void IsRunnableProject_GivenProjectFile_ReturnsExpected(string directory, string outputType, bool expected)
    {
        // Arrange
        var root = AbsolutePath.Create(Path.GetTempPath()) / Guid.NewGuid().ToString("N");
        var projectFile = root / directory / "App" / "App.csproj";
        projectFile.WriteAllText(
            $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>{outputType}</OutputType></PropertyGroup></Project>");

        try
        {
            // Act
            var result = IStandardBuild.IsRunnableProject(projectFile);

            // Assert
            result.Should().Be(expected);
        }
        finally
        {
            root.DeleteDirectory();
        }
    }
}
