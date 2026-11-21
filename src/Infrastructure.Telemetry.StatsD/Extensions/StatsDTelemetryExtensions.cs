using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StatsdClient;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.StatsD;

/// <summary>Registers DogStatsD as the toolkit's metric recorder.</summary>
public static class StatsDTelemetryExtensions
{
    /// <summary>Registers a container-owned client and selects it for toolkit metrics.</summary>
    /// <remarks>The client is configured when first resolved and flushed and disposed with the service provider.
    /// This replaces the active recorder, not existing custom <see cref="ICoreTelemetry"/> or <see cref="AppTelemetry"/>.
    /// Later recorder registrations take precedence.</remarks>
    public static IServiceCollection AddStatsDTelemetry(this IServiceCollection services, StatsdConfig config,
                                                       bool useDistributions = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.AddSingleton<IDogStatsd>(provider => CreateClient(config, provider.GetRequiredService<ILoggerFactory>()));
        services.AddSingleton<IMetricRecorder>(provider =>
            new StatsDMetricRecorder(provider.GetRequiredService<IDogStatsd>(), useDistributions));
        return services.AddCoreTelemetry();
    }

    private static IDogStatsd CreateClient(StatsdConfig config, ILoggerFactory loggerFactory)
    {
        var client = new DogStatsdService();
        var logger = loggerFactory.CreateLogger<StatsDMetricRecorder>();
        Exception? error = null;
        if (client.Configure(config, exception => {
            error = exception;
            logger.LogWarning(exception, "DogStatsD client reported an error");
        }))
            return client;

        client.Dispose();
        throw new InvalidOperationException("Could not configure the DogStatsD client.", error);
    }
}
