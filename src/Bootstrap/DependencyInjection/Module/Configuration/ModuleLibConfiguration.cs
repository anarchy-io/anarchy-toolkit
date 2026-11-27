using Anarchy.Toolkit.Bootstrap.Core.Service;
using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.Service;
using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Service.AppSecret;
using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.DependencyInjection.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration;

/// <summary>
/// Registers the application settings, telemetry and secret management.
/// </summary>
/// <remarks>
/// The telemetry module is registered after settings and before secret management. Secret management is
/// provider-neutral by default: <see cref="InMemoryAppSecretService"/> is registered in local environments and no
/// remote <see cref="ISecretManagementService"/> is registered; opt into one with
/// <see cref="WithSecretManagement"/> (for example <c>ModuleAwsSecretManagement</c> from
/// <c>Anarchy.Toolkit.Bootstrap.Aws</c>). Logging providers, a metric
/// recorder or an <see cref="ICoreTelemetry"/> registered by the host are kept; pass a <see cref="ModuleTelemetry"/>
/// built with the application's <see cref="ModuleTelemetryBase"/> to register them as a module.
/// Modules are registered in this order: <typeparamref name="TAppSettingsDiModule"/>, <see cref="ModuleTelemetry"/>,
/// then secret management (<see cref="ISecretManagementService"/>).
/// </remarks>
/// <typeparam name="TAppSettingsDiModule">The module that binds and registers <typeparamref name="TAppSettings"/>
/// (typically derived from <see cref="Entity.ModuleAppSettingsBase{TAppSettings}"/>).</typeparam>
/// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
/// <example>
/// <code language="csharp">
/// services.UseModule(new ModuleLibConfiguration&lt;ModuleAppSettings, AppSettings&gt;(), configuration, environment);
///
/// // Remote environments decrypt secrets with AWS KMS (Anarchy.Toolkit.Bootstrap.Aws):
/// services.UseModule(
///     new ModuleLibConfiguration&lt;ModuleAppSettings, AppSettings&gt;()
///         .WithSecretManagement(new ModuleAwsSecretManagement()),
///     configuration, environment);
/// </code>
/// </example>
public sealed class ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings> : IDependencyInjectionModule
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    private readonly ModuleTelemetry _telemetry;
    private IDependencyInjectionModule? _secretManagement;

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

    /// <summary>
    /// Registers <paramref name="secretManagement"/> as the secret management module, applied after telemetry and
    /// before the local <see cref="InMemoryAppSecretService"/> fallback (registered only when no secret service is
    /// registered yet).
    /// </summary>
    /// <param name="secretManagement">The module registering an <see cref="ISecretManagementService"/>.</param>
    /// <returns>This module, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="secretManagement"/> is <see langword="null"/>.</exception>
    public ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings> WithSecretManagement(
        IDependencyInjectionModule secretManagement)
    {
        _secretManagement = secretManagement ?? throw new ArgumentNullException(nameof(secretManagement));
        return this;
    }

    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services
            .UseModule<TAppSettingsDiModule>(configuration, environment)
            .UseModule(_telemetry, configuration, environment)
            ;

        if (_secretManagement is not null)
        {
            services.UseModule(_secretManagement, configuration, environment);
        }

        if (environment.IsLocal())
        {
            services.TryAddSingleton<ISecretManagementService, InMemoryAppSecretService>();
        }

        return services;
    }
}
