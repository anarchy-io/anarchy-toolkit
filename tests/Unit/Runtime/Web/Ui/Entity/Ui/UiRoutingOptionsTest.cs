using Anarchy.Toolkit.Runtime.Web.Ui.Entity.Ui;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Ui.Entity.Ui;

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
