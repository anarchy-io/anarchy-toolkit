using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;

/// <summary>Registers Serilog as an optional provider for application telemetry.</summary>
public static class SerilogTelemetryExtensions
{
    /// <summary>
    /// Adds the supplied Serilog logger as a Microsoft logging provider without clearing other providers or replacing
    /// an existing application telemetry registration. The application owns the logger and its sinks.
    /// </summary>
    /// <remarks>The application must also register its preferred metrics infrastructure, including an <c>IMeterFactory</c>.</remarks>
    public static IServiceCollection AddSerilogTelemetry(this IServiceCollection services, global::Serilog.ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);
        services.AddLogging();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(new SerilogLoggerProvider(logger, dispose: false)));
        return services.AddAppTelemetry();
    }
}
