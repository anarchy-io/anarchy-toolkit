using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Service;

public class BuildHostScaffolderTest
{
    [Fact]
    public void Create_GivenAutomationBuildVersion_WritesTheAnarchyHostAndPackageReference()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-host-" + Guid.NewGuid());
        try
        {
            BuildHostScaffolder.Create(root, automationBuildVersion: "1.2.3");

            ToolkitPackageVersions.AutomationBuild.Should().Be("Anarchy.Toolkit.Automation.Build");
            typeof(ToolkitPackageVersions).GetField("BuildFallout").Should().BeNull();
            typeof(BuildHostScaffolder).GetMethod(nameof(BuildHostScaffolder.Create))!.GetParameters()[1].Name
                .Should().Be("automationBuildVersion");
            File.ReadAllText(Path.Combine(root, "build", "_build.csproj"))
                .Should().Contain("<PackageReference Include=\"Anarchy.Toolkit.Automation.Build\" Version=\"1.2.3\" />");
            File.ReadAllText(Path.Combine(root, "build", "Program.cs"))
                .Should().Contain("class Build : AnarchyBuild,").And.NotContain("using Fallout.Common;");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
