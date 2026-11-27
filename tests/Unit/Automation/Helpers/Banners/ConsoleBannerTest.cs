using Anarchy.Toolkit.Automation.Helpers.Banners;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Helpers.Banners;

public class ConsoleBannerTest
{
    [Fact]
    public void Render_PicksTheWordmarkOnWideTerminalsTheEmblemOnMediumOnesAndATitleOnNarrowOnes()
    {
        ConsoleBanner.Render(ConsoleBanner.WideColumns).Should().Contain("▄████████").And.Contain("\e[0;33m").And.EndWith("\e[0m\n");
        ConsoleBanner.Render(ConsoleBanner.WideColumns - 1).Should().Contain("=**=").And.NotContain("▄████████");
        ConsoleBanner.Render(80, color: false).Should().NotContain("\e[");
        ConsoleBanner.Render(ConsoleBanner.CompactColumns).Should().Contain("=**=");
        ConsoleBanner.Render(ConsoleBanner.CompactColumns - 1).Should().Contain("TOOLKIT").And.NotContain("=**=");
        ConsoleBanner.Render(ConsoleBanner.CompactColumns - 1, color: false).Should().Be($"\n {ConsoleBanner.Title}\n\n");
    }
}
