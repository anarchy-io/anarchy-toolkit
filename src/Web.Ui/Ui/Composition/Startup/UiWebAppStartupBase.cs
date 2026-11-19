using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Web.Composition;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;
using Anarchy.Toolkit.Web.Ui.Ui.Extension.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Web.Ui.Ui.Composition.Startup;

/// <summary>
/// Startup base of an interactive-server Blazor UI with an optional cookie session.
/// </summary>
/// <remarks>
/// <para>
/// On top of <see cref="Anarchy.Toolkit.Web.Composition.WebAppStartupBase{TAppSettingsDiModule, TAppSettings}"/> it
/// registers the toolkit UI (<c>AddToolkitUi</c>) with the routing options of <see cref="GetUiRoutingOptions"/>,
/// the cookie session add-on when <see cref="GetUiAuthenticationOptions"/> returns options (it returns
/// <see langword="null"/> by default, so the UI has no login), and whatever <see cref="ConfigureUi"/> adds.
/// </para>
/// <para>
/// <see cref="Configure"/> runs <c>UseToolkitHostUi</c>, maps the toolkit UI endpoints (<c>MapToolkitUi</c>: login/logout when the cookie
/// session is enabled), and then calls <see cref="ConfigureUiEndpoints"/>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// public class Startup(IWebHostEnvironment environment, IConfiguration configuration)
///     : UiWebAppStartupBase&lt;MyAppSettingsDiModule, MyAppSettings&gt;(environment, configuration)
/// {
///     protected override string ApiName =&gt; "My UI";
///
///     protected override UiRoutingOptions GetUiRoutingOptions() =&gt; new() { EnableEnvironmentNamePathPrefix = true };
///
///     protected override void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints)
///     {
///         endpoints.MapRazorComponents&lt;App&gt;().AddInteractiveServerRenderMode();
///     }
/// }
/// </code>
/// </example>
/// <typeparam name="TAppSettingsDiModule">The DI module that binds and registers <typeparamref name="TAppSettings"/>.</typeparam>
/// <typeparam name="TAppSettings">The application settings type.</typeparam>
/// <param name="environment">The web hosting environment.</param>
/// <param name="configuration">The application configuration.</param>
public abstract class UiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>(
    IWebHostEnvironment environment,
    IConfiguration configuration)
    : WebAppStartupBase<TAppSettingsDiModule, TAppSettings>(environment, configuration)
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    /// <summary>
    /// Override to opt into the cookie session: return its options to add the OAuth login and environment catalog.
    /// </summary>
    /// <returns>The UI authentication options; <see langword="null"/> (no session) by default.</returns>
    protected virtual UiAuthenticationOptions? GetUiAuthenticationOptions() => null;

    /// <summary>
    /// Override to change how rendered links are built.
    /// </summary>
    /// <returns>The UI routing options; defaults to a new <see cref="UiRoutingOptions"/>.</returns>
    protected virtual UiRoutingOptions GetUiRoutingOptions() => new();

    /// <summary>
    /// Override to configure further UI add-ons. Runs after the routing options and, when enabled, the cookie session
    /// have been applied.
    /// </summary>
    /// <param name="ui">The toolkit UI builder.</param>
    protected virtual void ConfigureUi(ToolkitUiBuilder ui)
    {
    }

    /// <summary>Registers the toolkit UI (<c>AddToolkitUi</c>) in addition to the base services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        return base
            .ConfigureWebCustomServices(services)
            .AddToolkitUi(ui => {
                ui.UseRouting(GetUiRoutingOptions());

                if (GetUiAuthenticationOptions() is { } authOptions)
                {
                    ui.AddCookieSession(authOptions);
                }

                ConfigureUi(ui);
            });
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
    /// <param name="endpoints">The endpoint route builder.</param>
    protected abstract void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>Configures the UI pipeline (<c>UseToolkitHostUi</c>) and maps the session and UI endpoints.</summary>
    /// <param name="app">The application builder.</param>
    public override void Configure(IApplicationBuilder app)
    {
        app
            .UseToolkitHostUi(Environment)
            .UseEndpoints(endpoints => {
                endpoints.MapToolkitUi();
                ConfigureUiEndpoints(endpoints);
            })
            ;
    }

    /// <summary>
    /// Logs the environment name and the <c>RUNTIME_HOST</c> environment variable (or <c>unknown</c>) once the
    /// application has started. Not called automatically; invoke it from <see cref="Configure"/> overrides if desired.
    /// </summary>
    /// <param name="app">The application builder, used to resolve application telemetry and application lifetime.</param>
    /// <param name="caller">The call site attributed to the log entry.</param>
    protected void LogStartupInformation(IApplicationBuilder app, CallSite caller)
    {
        var telemetry = app.ApplicationServices.GetRequiredService<IAppTelemetry>();
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStarted.Register(() => {
            telemetry.Log(
                LogLevel.Information,
                caller,
                null,
                "Application has started with environment '{EnvironmentName}' and runtime host '{RuntimeHost}'",
                Environment.EnvironmentName,
                System.Environment.GetEnvironmentVariable("RUNTIME_HOST") ?? "unknown");
        });
    }
}
