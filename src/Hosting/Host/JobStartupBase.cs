using Anarchy.Toolkit.Configuration;
using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Hosting.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Hosting.Host;

/// <summary>
/// Generic startup base for Jobs and Workers. Composes the generic <see cref="DiFactory{TAppSettingsDiModule,TAppSettings}"/> registrations
/// and exposes a virtual hook for consumers to add their own services and domain modules.
/// </summary>
public abstract class JobStartupBase<TAppSettingsDiModule, TAppSettings> : IStartup
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    /// <summary>The application configuration.</summary>
    protected readonly IConfiguration Configuration;

    /// <summary>The host environment.</summary>
    protected readonly IHostEnvironment Environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStartupBase{TAppSettingsDiModule,TAppSettings}"/> class.
    /// </summary>
    /// <param name="environment">The host environment.</param>
    /// <param name="configuration">The application configuration.</param>
    [SuppressMessage("ReSharper", "ConvertToPrimaryConstructor")]
    protected JobStartupBase(IHostEnvironment environment, IConfiguration configuration)
    {
        Environment = environment;
        Configuration = configuration;
    }

    /// <summary>
    /// Registers the generic services via <see cref="DiFactory{TAppSettingsDiModule,TAppSettings}"/> then invokes
    /// <see cref="ConfigureJobCustomServices"/> for consumer-specific registrations.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureJobCustomServices(
                DiFactory<TAppSettingsDiModule, TAppSettings>.Build(services, Configuration, Environment))
            ;
    }

    /// <summary>
    /// Override to register consumer-specific services and domain modules.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for fluent chaining.</returns>
    protected virtual IServiceCollection ConfigureJobCustomServices(IServiceCollection services)
    {
        return services;
    }
}
