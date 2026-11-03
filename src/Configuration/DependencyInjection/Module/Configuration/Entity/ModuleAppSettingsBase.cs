using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity;

public class ModuleAppSettingsBase<TAppSettings> : IAppSettingsDiModule<TAppSettings>
    where TAppSettings : class, IAppSettings
{
    public IServiceCollection RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment _)
    {
        services
            .AddOptions<TAppSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Consumers depend on the capability interfaces (IAwsAppSettings, IPostgresAppSettings, ...) rather
        // than on the concrete AppSettings, so each section is resolvable on its own.
        var result = services
            .AddSingleton<TAppSettings>(sp => sp.GetRequiredService<IOptions<TAppSettings>>().Value)
            .AddSingleton<IAppSettings>(sp => sp.GetRequiredService<TAppSettings>())
            ;

        return RegisterAdditionalTypesForSettings(result);
    }

    /// <summary>
    /// Registers additional types for the specified AppSettings, for example capability interfaces (IAwsAppSettings,
    /// IPostgresAppSettings, ...) that are implemented by the TAppSettings.
    ///
    /// </summary>
    /// <param name="services">The service collection to register the types with.</param>
    /// <returns>The updated service collection.</returns>
    protected virtual IServiceCollection RegisterAdditionalTypesForSettings(IServiceCollection services)
    {
        return services;
    }
}
