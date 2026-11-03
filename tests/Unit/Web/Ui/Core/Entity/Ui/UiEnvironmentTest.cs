using Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Core.Entity.Ui;

public class UiEnvironmentTest
{
    private static readonly UiEnvironment Environment = new(
        "Testing",
        new Uri("https://auth.example.com/"),
        new Dictionary<string, Uri> { ["Orders"] = new("https://orders.example.com/") });

    [Fact]
    public void GetApiBaseUrl_ExactName_ReturnsUrl()
    {
        Environment.GetApiBaseUrl("Orders").Should().Be(new Uri("https://orders.example.com/"));
    }

    [Fact]
    public void GetApiBaseUrl_DifferentCasing_ReturnsUrl()
    {
        Environment.GetApiBaseUrl("orders").Should().Be(new Uri("https://orders.example.com/"));
    }

    [Fact]
    public void GetApiBaseUrl_UnknownApi_Throws()
    {
        var act = () => Environment.GetApiBaseUrl("Payments");

        act.Should().Throw<KeyNotFoundException>().WithMessage("*Payments*Testing*");
    }
}
