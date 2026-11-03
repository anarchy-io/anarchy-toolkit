using Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Extension.Service.Builder;

public class EmbeddedAssetsTest
{
    [Theory]
    [InlineData("toolkit-ui.css")]
    [InlineData("toolkit-ui.js")]
    public void Assembly_EmbedsUiStaticAsset(string fileName)
    {
        typeof(ApplicationBuilderExtension).Assembly.GetManifestResourceNames()
            .Should().Contain($"Anarchy.Toolkit.Web.Ui.wwwroot.{fileName}");
    }
}
