using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;

/// <summary>
/// Registers the application settings, observability and secret management.
/// </summary>
/// <remarks>
/// The parameterless constructor uses the vendor-neutral <see cref="ObserverFactory"/>; pass a
/// <see cref="ModuleObservability"/> to plug in an organisation's own observer.
/// </remarks>
public sealed class ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings> : IDependencyInjectionModule
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    private readonly ModuleObservability _observability;

    public ModuleLibConfiguration() : this(new ModuleObservability())
    {
    }

    public ModuleLibConfiguration(ModuleObservability observability)
    {
        _observability = observability ?? throw new ArgumentNullException(nameof(observability));
    }

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return services
            .UseModule<TAppSettingsDiModule>(configuration, environment)
            .UseModule(_observability, configuration, environment)
            .UseModule<ModuleSecretManagement>(configuration, environment)
            ;
    }
}
