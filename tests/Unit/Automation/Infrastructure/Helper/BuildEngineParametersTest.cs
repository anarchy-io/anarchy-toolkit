using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Infrastructure.Helper;

public class BuildEngineParametersTest
{
    [Fact]
    public void Read_UsesTheEngineOwnedParametersAndMatchesNamesCaseInsensitively()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-engine-parameters-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            BuildEngineParameters.DirectoryName.Should().Be(".fallout");
            typeof(BuildEngineParameters).Assembly.GetType("Anarchy.Toolkit.Automation.Infrastructure.Helper.FalloutParameters")
                .Should().BeNull();
            BuildEngineParameters.PathOf(root).Should().Be(Path.Combine(root, ".fallout", "parameters.json"));
            BuildEngineParameters.Read(root).Should().BeEmpty();

            Directory.CreateDirectory(Path.Combine(root, ".fallout"));
            File.WriteAllText(BuildEngineParameters.PathOf(root), "{\"Solution\":\"Example.sln\",\"Count\":1}");
            var parameters = BuildEngineParameters.Read(root);
            BuildEngineParameters.GetString(parameters, "solution").Should().Be("Example.sln");
            BuildEngineParameters.GetString(parameters, "count").Should().BeNull();
            BuildEngineParameters.GetString(parameters, "missing").Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
