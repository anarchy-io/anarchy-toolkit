using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Composition;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;
using Anarchy.Toolkit.Web.Ui.Ui.Extension.DependencyInjection;

namespace Anarchy.Toolkit.Web.Ui.Ui.Composition.Startup;

/// <summary>
/// Startup base of an interactive-server Blazor UI with an optional cookie session.
/// </summary>
public abstract class UiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>(
    IWebHostEnvironment environment,
    IConfiguration configuration)
    : WebAppStartupBase<TAppSettingsDiModule, TAppSettings>(environment, configuration)
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    /// <summary>
    /// Override to specify custom UI authentication options.
    /// Return null to disable authentication, or default options will be used.
    /// </summary>
    protected virtual UiAuthenticationOptions GetUiAuthenticationOptions() => new();

    /// <summary>
    /// Override to change how rendered links are built.
    /// </summary>
    protected virtual UiRoutingOptions GetUiRoutingOptions() => new();

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        base
            .ConfigureWebCustomServices(services)
            .AddHttpContextAccessor()
            .AddCascadingAuthenticationState();

        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddToolkitUiPresentation(GetUiRoutingOptions());

        var authOptions = GetUiAuthenticationOptions();
        if (authOptions != null)
        {
            services.AddToolkitUiAuthentication(authOptions);
        }

        return services;
    }

    /// <summary>
    /// Override to configure the UI endpoints (Razor components, controllers, static assets).
    /// Called within the UseEndpoints context, after the login/logout endpoints have been mapped.
    /// </summary>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// protected override void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints)
    /// {
    ///     endpoints
    ///         .MapRazorComponents&lt;App&gt;()
    ///         .AddInteractiveServerRenderMode();
    /// }
    /// </code>
    /// </remarks>
    protected abstract void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints);

    public override void Configure(IApplicationBuilder app)
    {
        app
            .UseToolkitHostUi(Environment)
            .UseEndpoints(endpoints => {
                if (endpoints.ServiceProvider.GetService<UiAuthenticationOptions>() is { EnableCookieAuth: true })
                {
                    endpoints.MapToolkitUiSessionEndpoints();
                }

                ConfigureUiEndpoints(endpoints);
            })
            ;
    }

    protected void LogStartupInformation(IApplicationBuilder app, CallSiteEntity caller)
    {
        var observer = app.ApplicationServices.GetRequiredService<Observer>();
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStarted.Register(() => {
            observer.LogInformation(
                caller,
                "Application has started with environment '{EnvironmentName}' and runtime host '{RuntimeHost}'",
                Environment.EnvironmentName,
                System.Environment.GetEnvironmentVariable("RUNTIME_HOST") ?? "unknown");
        });
    }
}
