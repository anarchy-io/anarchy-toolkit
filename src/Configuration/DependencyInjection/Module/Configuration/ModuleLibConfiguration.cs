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
/// Modules are registered in this order: <typeparamref name="TAppSettingsDiModule"/>, <see cref="ModuleObservability"/>,
/// then secret management (<see cref="Anarchy.Toolkit.Configuration.Core.Service.ISecretManagementService"/>).
/// </remarks>
/// <typeparam name="TAppSettingsDiModule">The module that binds and registers <typeparamref name="TAppSettings"/>
/// (typically derived from <see cref="Entity.ModuleAppSettingsBase{TAppSettings}"/>).</typeparam>
/// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
/// <example>
/// <code language="csharp">
/// services.UseModule(new ModuleLibConfiguration&lt;ModuleAppSettings, AppSettings&gt;(), configuration, environment);
/// </code>
/// </example>
public sealed class ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings> : IDependencyInjectionModule
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    private readonly ModuleObservability _observability;

    /// <summary>
    /// Creates the module with the default <see cref="ModuleObservability"/>.
    /// </summary>
    public ModuleLibConfiguration() : this(new ModuleObservability())
    {
    }

    /// <summary>
    /// Creates the module with a custom observability module.
    /// </summary>
    /// <param name="observability">The observability module to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="observability"/> is <see langword="null"/>.</exception>
    public ModuleLibConfiguration(ModuleObservability observability)
    {
        _observability = observability ?? throw new ArgumentNullException(nameof(observability));
    }

    /// <inheritdoc />
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
