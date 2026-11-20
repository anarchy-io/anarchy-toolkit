using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;

/// <summary>
/// Registers the application settings, telemetry and secret management.
/// </summary>
/// <remarks>
/// The telemetry module is registered after settings and before secret management. Logging providers, a metric
/// recorder or an <see cref="ICoreTelemetry"/> registered by the host are kept; pass a <see cref="ModuleTelemetry"/>
/// built with the application's <see cref="ModuleTelemetryBase"/> to register them as a module.
/// Modules are registered in this order: <typeparamref name="TAppSettingsDiModule"/>, <see cref="ModuleTelemetry"/>,
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
    private readonly ModuleTelemetry _telemetry;

    /// <summary>
    /// Creates the module with the default <see cref="ModuleTelemetry"/>.
    /// </summary>
    public ModuleLibConfiguration() : this(new ModuleTelemetry())
    {
    }

    /// <summary>
    /// Creates the module with a telemetry module, for example to register the application's logging and metrics or
    /// to customize correlation-id options.
    /// </summary>
    /// <param name="telemetry">The telemetry module to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="telemetry"/> is <see langword="null"/>.</exception>
    public ModuleLibConfiguration(ModuleTelemetry telemetry)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return services
            .UseModule<TAppSettingsDiModule>(configuration, environment)
            .UseModule(_telemetry, configuration, environment)
            .UseModule<ModuleSecretManagement>(configuration, environment)
            ;
    }
}
