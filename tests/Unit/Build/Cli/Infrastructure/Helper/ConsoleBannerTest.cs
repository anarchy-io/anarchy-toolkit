using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Infrastructure.Helper;

public class ConsoleBannerTest
{
    [Fact]
    public void Render_PicksTheWordmarkOnWideTerminalsAndTheEmblemOtherwise()
    {
        ConsoleBanner.Render(ConsoleBanner.WideColumns).Should().Contain("▄████████").And.Contain("\e[0;33m").And.EndWith("\e[0m\n");
        ConsoleBanner.Render(ConsoleBanner.WideColumns - 1).Should().Contain("=**=").And.NotContain("▄████████");
        ConsoleBanner.Render(80, color: false).Should().NotContain("\e[");
    }
}
