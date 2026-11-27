using Anarchy.Toolkit.Runtime.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Runtime.Web.Ui.Presentation.Presenter;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Ui.Presentation.Presenter;

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

    [Fact]
    public void GetRoute_WithPathBase_PrefixesPathBaseBeforeEnvironment()
    {
        var presenter = new BasePresenter(
            new UiRoutingOptions { PathBase = "/__mock/ui", EnableEnvironmentNamePathPrefix = true },
            Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Testing"));

        presenter.BaseHypertextReference.Should().Be("/__mock/ui/testing");
        presenter.GetUiRoute("orders").Should().Be("/__mock/ui/testing/ui/orders");
    }

    [Fact]
    public void GetRoute_WithPathBaseOnLocalHost_BaseHrefEndsWithSlash()
    {
        var presenter = new BasePresenter(
            new UiRoutingOptions { PathBase = "/__mock/ui" },
            Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Development"));

        presenter.GetRoute().Should().Be("/__mock/ui/");
        presenter.GetRoute("css/toolkit-ui.css").Should().Be("/__mock/ui/css/toolkit-ui.css");
    }
}
