using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Web.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.Extension.Service.Builder;

namespace Anarchy.Toolkit.Web.Composition.Startup;

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

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        var appSettings = Configuration.BuildValidAppSettings<TAppSettings>();

        return base
                .ConfigureWebCustomServices(services)
                .RegisterSwagger(ApiName, appSettings, Environment, titlePrefix: SwaggerTitlePrefix)
                .AddToolkitAuthentication(appSettings, Environment)
            ;
    }

    public override void Configure(IApplicationBuilder app)
    {
        app.UseToolkitHostRestApi(Environment, ApiName, SwaggerTitlePrefix);
    }
}
