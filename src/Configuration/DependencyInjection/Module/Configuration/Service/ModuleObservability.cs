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
public sealed class ModuleObservability : IDependencyInjectionModule
{
    private const string ConfigurationSectionName = "Observability";

    private readonly ObserverFactory _observerFactory;
    private readonly Func<IConfiguration, CorrelationIdOptions>? _correlationIdOptions;

    public ModuleObservability() : this(null)
    {
    }

    /// <param name="observerFactory">Builds the observer; the vendor-neutral <see cref="ObserverFactory"/> when <c>null</c>.</param>
    /// <param name="correlationIdOptions">Resolves the correlation-id options; the defaults when <c>null</c>.</param>
    public ModuleObservability(ObserverFactory? observerFactory,
                               Func<IConfiguration, CorrelationIdOptions>? correlationIdOptions = null)
    {
        _observerFactory = observerFactory ?? new ObserverFactory();
        _correlationIdOptions = correlationIdOptions;
    }

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
