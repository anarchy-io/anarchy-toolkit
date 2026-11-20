using Anarchy.Toolkit.Automation.Build.Component;
using AwesomeAssertions;
using Fallout.Common.IO;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Component;

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
    [InlineData("EndToEnd", "EndToEnd", true)]
    [InlineData("MyService.Tests.EndToEnd", "EndToEnd", true)]
    [InlineData("MyService.EndToEnd.Tests", "endtoend", true)]
    [InlineData("MyService.Tests.Acceptance", "EndToEnd", false)]
    public void MatchesTestKind_GivenProjectName_ReturnsExpected(string projectName, string kind, bool expected)
    {
        // Act
        var result = IStandardBuild.MatchesTestKind(projectName, kind);

        // Assert
        result.Should()
            .Be(expected);
    }

    [Fact]
    public void ParseTestKindAliases_GroupsAliasesByKind()
    {
        var aliases =
            IStandardBuild.ParseTestKindAliases(["EndToEnd=Acceptance", "endtoend = Smoke", "Integration=Contract"]);

        aliases["ENDTOEND"]
            .Should()
            .Equal("Acceptance", "Smoke");

        aliases["Integration"]
            .Should()
            .Equal("Contract");

        aliases["Integration"]
            .Any(alias => IStandardBuild.MatchesTestKind("Api.Tests.Contract", alias))
            .Should()
            .BeTrue();

        var invalid = () => IStandardBuild.ParseTestKindAliases(["Acceptance"]);
        invalid.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Kind=Alias*");
    }

    [Fact]
    public void ParseAppNames_GivenCommaSeparatedNames_TrimsAndDeduplicates()
    {
        // Act
        var result = IStandardBuild.ParseAppNames(" Orders, Payments,,orders ");

        // Assert
        result.Should()
            .Equal("Orders", "Payments");
    }

    [Theory]
    [InlineData("src", "Exe", true)]
    [InlineData("src", "Library", false)]
    [InlineData("tests", "Exe", false)]
    public void IsRunnableProject_GivenProjectFile_ReturnsExpected(string directory, string outputType, bool expected)
    {
        // Arrange
        var root = AbsolutePath.Create(Path.GetTempPath()) / Guid.NewGuid()
            .ToString("N");

        var projectFile = root / directory / "App" / "App.csproj";
        projectFile.WriteAllText(
            $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>{outputType}</OutputType></PropertyGroup></Project>");

        try
        {
            // Act
            var result = IStandardBuild.IsRunnableProject(projectFile);

            // Assert
            result.Should()
                .Be(expected);
        }
        finally
        {
            root.DeleteDirectory();
        }
    }

    [Fact]
    public void IsRunnableProject_GivenSourceDirectory_OnlyAcceptsProjectsBelowIt()
    {
        var root = AbsolutePath.Create(Path.GetTempPath()) / Guid.NewGuid()
            .ToString("N");

        var inCode = root / "code" / "App" / "App.csproj";
        var inSrc = root / "src" / "App" / "App.csproj";
        foreach (var project in new[] {
                     inCode,
                     inSrc
                 })
            project.WriteAllText("<Project><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup></Project>");

        try
        {
            IStandardBuild.IsRunnableProject(inCode, root / "code")
                .Should()
                .BeTrue();

            IStandardBuild.IsRunnableProject(inSrc, root / "code")
                .Should()
                .BeFalse();
        }
        finally
        {
            root.DeleteDirectory();
        }
    }
}
