using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.AspNetCore.Builder;
using Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Runtime.Web.Composition.Startup;

/// <summary>
/// Startup base class for toolkit REST APIs: adds JWT bearer authentication and the REST request pipeline
/// on top of <see cref="Anarchy.Toolkit.Runtime.Web.Composition.WebAppStartupBase{TAppSettingsDiModule, TAppSettings}"/>.
/// </summary>
/// <remarks>
/// Application settings are validated with <c>BuildValidAppSettings&lt;TAppSettings&gt;()</c>, exposed as
/// <see cref="AppSettings"/> and used for <c>AddToolkitAuthentication</c>. The pipeline is configured with
/// <c>UseToolkitHostRestApi</c>. Authorization policies are not registered automatically; call
/// <c>SetupAuthorization</c> from an override of <see cref="ConfigureWebCustomServices"/> when needed. Swagger is
/// opt-in: reference <c>Anarchy.Toolkit.Runtime.Web.Swagger</c> and call its <c>AddToolkitSwagger</c> from the same
/// override.
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
///             .SetupAuthorization("my-api.read", "my-api.write", "my-api.admin")
///             .AddToolkitSwagger(ApiName, AppSettings, Environment); // Anarchy.Toolkit.Runtime.Web.Swagger
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
    where TAppSettings : class, IAppSettings, new()
{
    private TAppSettings? _appSettings;

    /// <summary>
    /// The validated application settings, read from <see cref="WebAppStartupBase{TAppSettingsDiModule, TAppSettings}.Configuration"/> once.
    /// </summary>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">Thrown when the settings are invalid.</exception>
    protected TAppSettings AppSettings => _appSettings ??= Configuration.BuildValidAppSettings<TAppSettings>();

    /// <summary>
    /// Registers authentication in addition to the base services when the settings implement
    /// <see cref="IAuthServiceAppSettings"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        var result = base.ConfigureWebCustomServices(services);

        return AppSettings is IAuthServiceAppSettings authSettings
            ? result.AddToolkitAuthentication(authSettings, Environment)
            : result;
    }

    /// <summary>Configures the REST API pipeline through <c>UseToolkitHostRestApi</c>.</summary>
    /// <param name="app">The application builder.</param>
    public override void Configure(IApplicationBuilder app)
    {
        app.UseToolkitHostRestApi(Environment);
    }
}
