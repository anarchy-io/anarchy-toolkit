using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Entity.Ui;

public class UiRoutingOptionsTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("/", "")]
    [InlineData("__mock/ui", "/__mock/ui")]
    [InlineData("/__mock/ui/", "/__mock/ui")]
    [InlineData("/__mock/ui", "/__mock/ui")]
    public void PathBase_IsNormalized(string value, string expected)
    {
        new UiRoutingOptions { PathBase = value }.PathBase.Should().Be(expected);
    }
}
