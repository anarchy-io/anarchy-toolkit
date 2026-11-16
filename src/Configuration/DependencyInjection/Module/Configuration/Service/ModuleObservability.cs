using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Configuration.Entity.Observability;
using Anarchy.Toolkit.Configuration.Host;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.DependencyInjection.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;

/// <summary>
/// Registers the <see cref="Observer"/> singleton, the <see cref="IApplicationContext"/> and the correlation-id options.
/// </summary>
/// <remarks>
/// The observer is built by an <see cref="ObserverFactory"/>; organisation-specific sinks, enrichers and metrics
/// backends are plugged in by passing a derived factory.
/// </remarks>
/// <example>
/// <code language="csharp">
/// services.UseModule(
///     new ModuleLibConfiguration&lt;ModuleAppSettings, AppSettings&gt;(new ModuleObservability(new MyObserverFactory())),
///     configuration, environment);
/// </code>
/// </example>
public sealed class ModuleObservability : IDependencyInjectionModule
{
    private const string ConfigurationSectionName = "Observability";

    private readonly ObserverFactory _observerFactory;
    private readonly Func<IConfiguration, CorrelationIdOptions>? _correlationIdOptions;

    /// <summary>
    /// Creates the module with the default <see cref="ObserverFactory"/> and correlation-id options.
    /// </summary>
    public ModuleObservability() : this(null)
    {
    }

    /// <summary>
    /// Creates the module with a custom observer factory and/or correlation-id options.
    /// </summary>
    /// <param name="observerFactory">Builds the observer; the vendor-neutral <see cref="ObserverFactory"/> when <c>null</c>.</param>
    /// <param name="correlationIdOptions">Resolves the correlation-id options; the defaults when <c>null</c>.</param>
    public ModuleObservability(ObserverFactory? observerFactory,
                               Func<IConfiguration, CorrelationIdOptions>? correlationIdOptions = null)
    {
        _observerFactory = observerFactory ?? new ObserverFactory();
        _correlationIdOptions = correlationIdOptions;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads <see cref="ObservabilityOptions"/> from the <c>Observability</c> section to resolve the application name,
    /// registers the correlation-id module, <see cref="IApplicationContext"/>, clears the Microsoft logging providers,
    /// and registers <see cref="Observer"/> as a singleton built by the <see cref="ObserverFactory"/> (using the
    /// <see cref="AppRunner"/> from DI when present).
    /// </remarks>
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        var options = configuration.GetSection(ConfigurationSectionName).Get<ObservabilityOptions>()
                      ?? new ObservabilityOptions();

        var appName = AppName(environment, options.AppNamePrefix);

        return services
            .UseModule(new ModuleCorrelationId(_correlationIdOptions), configuration, environment)
            .AddSingleton<IApplicationContext, ApplicationContext>()
            .AddLogging(builder => builder.ClearProviders())
            .AddSingleton<Observer>(sp => _observerFactory.Create(new ObserverContext(
                sp, configuration, environment, appName, sp.GetService<AppRunner>()?.Type)));
    }

    private static string AppName(IHostEnvironment environment, string appNamePrefix)
    {
        var applicationName = !string.IsNullOrWhiteSpace(environment.ApplicationName)
            ? environment.ApplicationName
            : Assembly.GetEntryAssembly()?.GetName().Name
              ?? Assembly.GetExecutingAssembly().GetName().Name
              ?? appNamePrefix.TrimEnd('.');

        var trimmedPrefix = appNamePrefix.TrimEnd('.');

        return string.IsNullOrEmpty(trimmedPrefix)
               || applicationName.StartsWith(trimmedPrefix, StringComparison.OrdinalIgnoreCase)
            ? applicationName
            : $"{appNamePrefix}{applicationName}";
    }
}
