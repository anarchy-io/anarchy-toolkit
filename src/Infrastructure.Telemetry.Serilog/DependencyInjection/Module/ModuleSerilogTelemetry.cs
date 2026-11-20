using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;

/// <summary>Registers an application-owned Serilog logger as the toolkit telemetry's logging provider.</summary>
/// <param name="logger">The configured logger; it is not disposed by the toolkit.</param>
public sealed class ModuleSerilogTelemetry(global::Serilog.ILogger logger) : ModuleTelemetryBase
{
    /// <inheritdoc />
    protected override void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration,
                                                  IHostEnvironment environment) =>
        services.AddSerilogTelemetry(logger);
}
