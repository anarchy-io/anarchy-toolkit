using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StatsdClient;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.StatsD;

/// <summary>Registers DogStatsD metrics using configuration supplied by the composition root.</summary>
public sealed class ModuleStatsDTelemetry(Func<IConfiguration, StatsdConfig> config, bool useDistributions = false)
    : ModuleTelemetryBase
{
    /// <inheritdoc />
    protected override void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration,
                                                  IHostEnvironment environment) =>
        services.AddStatsDTelemetry((config ?? throw new ArgumentNullException(nameof(config)))(configuration),
            useDistributions);
}
