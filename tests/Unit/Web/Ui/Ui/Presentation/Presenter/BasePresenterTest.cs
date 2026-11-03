using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Ui.Presentation.Presenter;

public class BasePresenterTest
{
    private static BasePresenter Create(string environment, bool prefix) => new(
        new UiRoutingOptions { EnableEnvironmentNamePathPrefix = prefix },
        Mock.Of<IHostEnvironment>(x => x.EnvironmentName == environment));

    [Fact]
    public void GetRoute_TapHostWithPrefixing_PrefixesLowercaseEnvironment()
    {
        var presenter = Create("Testing", prefix: true);

        presenter.GetRoute("favicon.png").Should().Be("/testing/favicon.png");
        presenter.GetUiRoute("orders").Should().Be("/testing/ui/orders");
        presenter.GetEndpointRoute("/auth/login").Should().Be("/testing/endpoint/auth/login");
    }

    [Fact]
    public void GetRoute_LocalHost_NeverPrefixes()
    {
        var presenter = Create("Development", prefix: true);

        presenter.GetRoute().Should().Be("/");
        presenter.GetUiRoute().Should().Be("/ui");
    }

    [Fact]
    public void GetRoute_PrefixingDisabled_DoesNotPrefix()
    {
        Create("Production", prefix: false).GetUiRoute("login").Should().Be("/ui/login");
    }
}
