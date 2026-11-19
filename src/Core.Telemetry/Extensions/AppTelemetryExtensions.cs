using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Registers the toolkit's application telemetry facade.</summary>
public static class AppTelemetryExtensions
{
    /// <summary>
    /// Adds the default facade without replacing a consumer registration. The application supplies logging and
    /// <see cref="System.Diagnostics.Metrics.IMeterFactory"/> through its existing DI registrations.
    /// </summary>
    public static IServiceCollection AddAppTelemetry(this IServiceCollection services)
    {
        services.TryAddSingleton<AppTelemetryFactory>();
        services.TryAddSingleton<IAppTelemetry>(provider => provider.GetRequiredService<AppTelemetryFactory>().Create());
        return services;
    }

    /// <summary>
    /// Adds an opt-in hosted listener that forwards toolkit metrics to the registered logging providers. The host
    /// controls listener lifetime, and other metric collectors remain independent.
    /// </summary>
    public static IServiceCollection AddTelemetryLogMetrics(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, LogMetricListener>());
        return services;
    }
}
