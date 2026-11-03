using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Service.AppSecret;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.DependencyInjection;

/// <summary>
/// Composes the generic cross-cutting service registrations shared by all Jobs and Workers.
/// </summary>
/// <remarks>
/// This factory only wires the generic <typeparamref name="TAppSettingsDiModule"/> module plus a small set of
/// cross-cutting singletons. Domain-specific modules (infrastructure, aggregates, brokers, etc.) are supplied
/// by the consumer through the <c>extraModules</c> parameter.
/// </remarks>
public static class DiFactory<TAppSettingsDiModule, TAppSettings>
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    /// <summary>
    /// Registers the generic configuration module and cross-cutting services, followed by any
    /// consumer-supplied dependency-injection modules.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <param name="extraModules">
    /// Additional consumer-supplied modules (for example infrastructure or domain modules) to register
    /// after the generic configuration module.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for fluent chaining.</returns>
    public static IServiceCollection Build(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        params IDependencyInjectionModule[] extraModules)
    {
        ServiceCollectionServiceExtensions

            // Generic cross-cutting registrations
            .AddSingleton<CorrelationIdOptions>(services
                .AddSingleton(environment)
                .AddMemoryCache()
                .UseModule<TAppSettingsDiModule>(configuration, environment))
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddScoped<ICachedSecretManagementService, CachedSecretManagementService>()
            .AddSingleton(TimeProvider.System)
            ;

        foreach (var module in extraModules)
        {
            module.RegisterServices(services, configuration, environment);
        }

        return services;
    }
}
