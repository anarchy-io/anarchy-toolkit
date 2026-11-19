using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using AwesomeAssertions;
using Spectre.Console;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Infrastructure.Helper;

public class ConsoleBannerOptionsTest
{
    [Fact]
    public void Defaults_ShowAnarchysBanner()
    {
        var options = new ConsoleBannerOptions();

        options.Hide.Should().BeFalse();
        options.Banner.Should().BeSameAs(AnarchyConsoleBanner.Default);
        AnarchyConsoleBanner.Default.Render(80, color: false).Should().Be(ConsoleBanner.Render(80, color: false));
    }

    [Fact]
    public void Write_PrintsNothingWhenHidden()
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });

        new ConsoleBannerOptions { Banner = new FixedBanner(), Hide = true }.Write(console);

        output.ToString().Should().BeEmpty();
    }

    private sealed class FixedBanner : IConsoleBanner
    {
        public string Render(int columns, bool color = true) => "custom\n";
    }
}
