using Anarchy.Toolkit.Runtime.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Runtime.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Runtime.Web.Ui.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Runtime.Web.Ui.Presentation.Presenter;
using Anarchy.Toolkit.Runtime.Web.Ui.Presentation.Service;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Ui.Extensions.Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddToolkitUi_OnBareServiceCollection_RegistersPresentationServices()
    {
        var services = new ServiceCollection().AddToolkitUi();

        services.Should().Contain(d => d.ServiceType == typeof(JsonModalState));
        services.Should().Contain(d => d.ServiceType == typeof(NotificationService));
        services.Should().Contain(d => d.ServiceType == typeof(BasePresenter));
        services.Should().Contain(d => d.ServiceType == typeof(JsonRenderPresenter));
        services.Should().Contain(d => d.ServiceType == typeof(IHttpContextAccessor));
        services.Should().Contain(d => d.ServiceType == typeof(UiRoutingOptions));
    }

    [Fact]
    public void AddToolkitUi_ByDefault_DoesNotRegisterSession()
    {
        var services = new ServiceCollection().AddToolkitUi();

        services.Should().NotContain(d => d.ServiceType == typeof(UiAuthenticationOptions));
        services.Should().NotContain(d => d.ServiceType == typeof(IUiEnvironmentCatalog));
        services.Should().NotContain(d => d.ServiceType == typeof(LoginPresenter));
    }

    [Fact]
    public void AddToolkitUi_WithUseRouting_RegistersGivenRoutingOptions()
    {
        var routing = new UiRoutingOptions { UiRoutePrefix = "admin" };

        using var provider = new ServiceCollection().AddToolkitUi(ui => ui.UseRouting(routing)).BuildServiceProvider();

        provider.GetRequiredService<UiRoutingOptions>().Should().BeSameAs(routing);
    }

    [Fact]
    public void AddCookieSession_RegistersSessionServices()
    {
        var options = new UiAuthenticationOptions();

        var services = new ServiceCollection().AddToolkitUi(ui => ui.AddCookieSession(options));

        services.Should().Contain(d => d.ServiceType == typeof(UiAuthenticationOptions) && ReferenceEquals(d.ImplementationInstance, options));
        services.Should().Contain(d => d.ServiceType == typeof(IUiEnvironmentCatalog));
        services.Should().Contain(d => d.ServiceType == typeof(AuthenticationStateProvider));
        services.Should().Contain(d => d.ServiceType == typeof(LoginPresenter));
    }

    [Fact]
    public void AddToolkitUiPresentation_DoesNotRegisterLoginPresenter()
    {
        var services = new ServiceCollection().AddToolkitUiPresentation();

        services.Should().NotContain(d => d.ServiceType == typeof(LoginPresenter));
    }
}
