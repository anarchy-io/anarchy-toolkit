using Anarchy.Toolkit.Runtime.Web.Ui.Extensions.Microsoft.AspNetCore.Builder;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Ui.Extensions.Microsoft.AspNetCore.Builder;

public class EmbeddedAssetsTest
{
    [Theory]
    [InlineData("css.toolkit-ui.css")]
    [InlineData("js.toolkit-ui.js")]
    public void Assembly_EmbedsUiStaticAsset(string fileName)
    {
        typeof(ApplicationBuilderExtensions).Assembly.GetManifestResourceNames()
            .Should().Contain($"Anarchy.Toolkit.Runtime.Web.Ui.wwwroot.{fileName}");
    }
}
