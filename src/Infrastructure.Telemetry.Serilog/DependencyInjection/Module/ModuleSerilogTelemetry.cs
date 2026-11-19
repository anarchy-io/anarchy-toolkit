using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;

/// <summary>Registers an application-owned Serilog logger for the toolkit telemetry facade.</summary>
public sealed class ModuleSerilogTelemetry(global::Serilog.ILogger logger) : IDependencyInjectionModule
{
    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services.AddSerilogTelemetry(logger);
}
