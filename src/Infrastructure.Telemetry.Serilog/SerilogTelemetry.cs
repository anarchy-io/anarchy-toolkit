using Anarchy.Toolkit.Core.Telemetry;
using Serilog.Extensions.Logging;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;

/// <summary>Builds the toolkit's telemetry over a Serilog logger without dependency injection, for console tools.</summary>
public static class SerilogTelemetry
{
    /// <summary>Creates an <see cref="AppTelemetry"/> that writes to <paramref name="logger"/>.</summary>
    /// <remarks>The logger stays owned by the caller and is never disposed here.</remarks>
    /// <param name="logger">The Serilog logger.</param>
    /// <param name="metrics">The metric recorder; metrics are discarded when <see langword="null"/>.</param>
    /// <returns>The telemetry.</returns>
    public static AppTelemetry Create(global::Serilog.ILogger logger, IMetricRecorder? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var loggerFactory = new SerilogLoggerFactory(logger, dispose: false);
        return new AppTelemetry(new CoreTelemetry(loggerFactory.CreateLogger(CoreTelemetry.InstrumentationName),
            metrics ?? NullMetricRecorder.Instance));
    }
}
