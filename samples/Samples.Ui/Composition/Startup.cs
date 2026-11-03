using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Samples.Ui.Composition.DependencyInjection;
using Anarchy.Toolkit.Samples.Ui.Core.Service;
using Anarchy.Toolkit.Samples.Ui.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Samples.Ui.Infrastructure.Service.Http.Client;
using Anarchy.Toolkit.Samples.Ui.Ui.Presentation.Presenter;
using Anarchy.Toolkit.Samples.Ui.Ui.Template;
using Anarchy.Toolkit.Samples.Ui.Web.Stub;
using Anarchy.Toolkit.Web.Ui.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Ui.Composition.Startup;

namespace Anarchy.Toolkit.Samples.Ui.Composition;

public sealed class Startup(IWebHostEnvironment environment, IConfiguration configuration)
    : UiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override string ApiName => "Samples UI";

    /// <summary>
    /// Two environments, both served by the stubs in this host so the sample runs on its own. A real UI points
    /// each one at that environment's auth service and API hosts.
    /// </summary>
    protected override UiAuthenticationOptions GetUiAuthenticationOptions()
    {
        var baseUrl = (Configuration.Get<AppSettings>() ?? new AppSettings()).StubBaseUrl;

        return new UiAuthenticationOptions {
            Scope = "orders:admin",
            Environments = [
                new UiEnvironment("Development", new Uri(baseUrl, "stub/auth/"),
                    new Dictionary<string, Uri> { [OrdersApiClient.ApiName] = new(baseUrl, "stub/development/") }),
                new UiEnvironment("Testing", new Uri(baseUrl, "stub/auth/"),
                    new Dictionary<string, Uri> { [OrdersApiClient.ApiName] = new(baseUrl, "stub/testing/") }),
            ]
        };
    }

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        base.ConfigureWebCustomServices(services)
            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment)
            .AddToolkitUiApiHttpClient(TimeSpan.FromSeconds(30));

        return services
            .AddSingleton<StubOrderStore>()
            .AddScoped<IOrdersApiClient, OrdersApiClient>()
            .AddScoped<OrdersPresenter>();
    }

    protected override void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints)
    {
        if (Environment.IsLocal())
        {
            endpoints.MapStubEndpoints();
        }

        endpoints.MapStaticAssets();
        endpoints
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }

    public override void Configure(IApplicationBuilder app)
    {
        base.Configure(app);

        LogStartupInformation(app, this.CalledFrom());
    }
}
