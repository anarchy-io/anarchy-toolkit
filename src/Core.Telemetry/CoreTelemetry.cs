using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>The default <see cref="ICoreTelemetry"/>: holds a logger and a metric recorder supplied by the caller.</summary>
/// <remarks>Neither the logger nor the recorder is disposed here: their owner (usually the DI container) does it.</remarks>
/// <param name="logger">The logger toolkit packages write to.</param>
/// <param name="metrics">The recorder toolkit packages send metrics to.</param>
public sealed class CoreTelemetry(ILogger logger, IMetricRecorder metrics) : ICoreTelemetry
{
    /// <summary>The logger category and meter name the toolkit's telemetry uses.</summary>
    public const string InstrumentationName = "Anarchy.Toolkit.Telemetry";

    /// <summary>Discards every log and metric.</summary>
    public static CoreTelemetry Null { get; } = new(NullLogger.Instance, NullMetricRecorder.Instance);

    /// <inheritdoc />
    public ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public IMetricRecorder Metrics { get; } = metrics ?? throw new ArgumentNullException(nameof(metrics));

    /// <summary>
    /// Creates the primitives from existing logging and metrics infrastructure, without dependency injection (for
    /// console tools and tests).
    /// </summary>
    /// <param name="loggerFactory">Creates the <see cref="InstrumentationName"/> logger; owned by the caller.</param>
    /// <param name="meterFactory">Creates the <see cref="InstrumentationName"/> meter and owns it; metrics are discarded when <see langword="null"/>.</param>
    /// <returns>The primitives.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="loggerFactory"/> is <see langword="null"/>.</exception>
    public static CoreTelemetry Create(ILoggerFactory loggerFactory, IMeterFactory? meterFactory = null)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        return new CoreTelemetry(loggerFactory.CreateLogger(InstrumentationName),
            meterFactory is null ? NullMetricRecorder.Instance : new MeterMetricRecorder(meterFactory));
    }
}
