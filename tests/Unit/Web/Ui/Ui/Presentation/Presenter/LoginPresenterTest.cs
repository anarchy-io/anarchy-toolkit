using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Ui.Presentation.Presenter;

public class LoginPresenterTest
{
    private static LoginPresenter Create(string environment) => new(
        Mock.Of<IUiEnvironmentCatalog>(x => x.Names == new[] { "Development", "Testing" }),
        new UiRoutingOptions(),
        Mock.Of<IHostEnvironment>(x => x.EnvironmentName == environment));

    [Fact]
    public void Initialize_LocalHost_ShowsEnvironmentSelectorWithCatalogNames()
    {
        var presenter = Create("Development");

        presenter.Initialize();

        presenter.ShowEnvironmentSelector.Should().BeTrue();
        presenter.SelectedEnvironment.Should().Be("Development");
        presenter.AvailableEnvironments.Should().Equal("Development", "Testing");
    }

    [Fact]
    public void Initialize_TapHost_HidesEnvironmentSelector()
    {
        var presenter = Create("Testing");

        presenter.Initialize();

        presenter.ShowEnvironmentSelector.Should().BeFalse();
    }

    [Theory]
    [InlineData(UiLoginErrors.InvalidCredentials, "Client ID and client secret are required.")]
    [InlineData(UiLoginErrors.AuthFailed, "Authentication failed*")]
    [InlineData("unknown", "")]
    [InlineData(null, "")]
    public void SetErrorFromQueryParameter_MapsErrorCodes(string? code, string expected)
    {
        var presenter = Create("Development");

        presenter.SetErrorFromQueryParameter(code);

        if (expected.Length == 0)
        {
            presenter.ErrorMessage.Should().BeEmpty();
            return;
        }

        presenter.ErrorMessage.Should().Match(expected);
    }
}
