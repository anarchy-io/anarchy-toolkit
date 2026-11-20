using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;

/// <summary>
/// Readable console defaults for composition roots that own their output, such as command-line tools. Libraries never
/// apply them: a host that already configured logging keeps its own setup.
/// </summary>
public static class SerilogConsoleDefaults
{
    /// <summary>The default console output template.</summary>
    public const string OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>Applies the defaults: log context, the given minimum level and a themed console sink.</summary>
    /// <param name="configuration">The logger configuration.</param>
    /// <param name="minimumLevel">The minimum level written.</param>
    /// <returns>The logger configuration, for chaining.</returns>
    public static LoggerConfiguration Apply(LoggerConfiguration configuration,
                                            LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: OutputTemplate, theme: AnsiConsoleTheme.Literate);
    }

    /// <summary>Creates a console logger with the defaults, owned by the caller.</summary>
    /// <param name="minimumLevel">The minimum level written.</param>
    /// <returns>The logger.</returns>
    public static global::Serilog.Core.Logger CreateLogger(LogEventLevel minimumLevel = LogEventLevel.Information) =>
        Apply(new LoggerConfiguration(), minimumLevel).CreateLogger();

    /// <summary>
    /// Adds a Serilog console logger with the defaults, unless the application already registered a logging provider,
    /// then <see cref="TelemetryServiceCollectionExtensions.AddCoreTelemetry"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="minimumLevel">The minimum level written.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSerilogConsoleTelemetry(this IServiceCollection services,
                                                                LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerProvider))
            ? services.AddCoreTelemetry()
            : services.AddSerilogTelemetry(configuration => Apply(configuration, minimumLevel));
    }
}
