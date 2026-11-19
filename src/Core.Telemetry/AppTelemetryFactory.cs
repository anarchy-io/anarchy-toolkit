using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Creates the toolkit facade from the application's registered logging and meter factories.</summary>
public sealed class AppTelemetryFactory(ILoggerFactory loggerFactory, IMeterFactory meterFactory)
{
    /// <summary>The instrumentation name used for logger and meter selection.</summary>
    public const string InstrumentationName = "Anarchy.Toolkit.Telemetry";

    /// <summary>Creates a facade using the existing application logging and metrics infrastructure.</summary>
    public IAppTelemetry Create() => new AppTelemetry(loggerFactory.CreateLogger(InstrumentationName),
        meterFactory.Create(new MeterOptions(InstrumentationName)));
}
