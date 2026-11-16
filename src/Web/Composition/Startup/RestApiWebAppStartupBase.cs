using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Web.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.Extension.Service.Builder;

namespace Anarchy.Toolkit.Web.Composition.Startup;

/// <summary>
/// Startup base class for toolkit REST APIs: adds Swagger, JWT bearer authentication and the REST request pipeline
/// on top of <see cref="Anarchy.Toolkit.Web.Composition.WebAppStartupBase{TAppSettingsDiModule, TAppSettings}"/>.
/// </summary>
/// <remarks>
/// Application settings are validated with <c>BuildValidAppSettings&lt;TAppSettings&gt;()</c> and used for
/// <c>RegisterSwagger</c> and <c>AddToolkitAuthentication</c>. The pipeline is configured with
/// <c>UseToolkitHostRestApi</c>. Authorization policies are not registered automatically; call
/// <c>SetupAuthorization</c> from an override of <see cref="ConfigureWebCustomServices"/> when needed.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public class Startup(IWebHostEnvironment environment, IConfiguration configuration)
///     : RestApiWebAppStartupBase&lt;MyAppSettingsDiModule, MyAppSettings&gt;(environment, configuration)
/// {
///     protected override string ApiName =&gt; "My API";
///
///     protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
///     {
///         return base.ConfigureWebCustomServices(services)
///             .SetupAuthorization("my-api.read", "my-api.write", "my-api.admin");
///     }
/// }
/// </code>
/// </example>
/// <typeparam name="TAppSettingsDiModule">The DI module that binds and registers <typeparamref name="TAppSettings"/>.</typeparam>
/// <typeparam name="TAppSettings">The application settings type; must expose the auth service settings.</typeparam>
/// <param name="environment">The web hosting environment.</param>
/// <param name="configuration">The application configuration.</param>
public abstract class RestApiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>(
    IWebHostEnvironment environment,
    IConfiguration configuration)
    : WebAppStartupBase<TAppSettingsDiModule, TAppSettings>(environment, configuration)
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAuthServiceAppSettings, new()
{
    /// <summary>
    /// Bracketed prefix used in the Swagger title and document title (e.g. "[MyApi]").
    /// Consumers may override; defaults to "[API]".
    /// </summary>
    protected virtual string SwaggerTitlePrefix => "[API]";

    /// <summary>
    /// Registers Swagger and authentication in addition to the base services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        var appSettings = Configuration.BuildValidAppSettings<TAppSettings>();

        return base
                .ConfigureWebCustomServices(services)
                .RegisterSwagger(ApiName, appSettings, Environment, titlePrefix: SwaggerTitlePrefix)
                .AddToolkitAuthentication(appSettings, Environment)
            ;
    }

    /// <summary>Configures the REST API pipeline through <c>UseToolkitHostRestApi</c>.</summary>
    /// <param name="app">The application builder.</param>
    public override void Configure(IApplicationBuilder app)
    {
        app.UseToolkitHostRestApi(Environment, ApiName, SwaggerTitlePrefix);
    }
}
