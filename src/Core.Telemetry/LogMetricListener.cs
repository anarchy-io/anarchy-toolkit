using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Optionally writes toolkit metric measurements through the application's logging pipeline.</summary>
/// <remarks>
/// Only meters created by the supplied factory are observed, so independent service providers do not share metrics.
/// This hosted listener is opt-in and does not replace other metric collectors.
/// </remarks>
public sealed class LogMetricListener(ILogger<LogMetricListener> logger, IMeterFactory meterFactory) : IHostedService, IDisposable
{
    private readonly MeterListener _listener = new();
    private int _started;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) return Task.CompletedTask;
        _listener.InstrumentPublished = (instrument, listener) => {
            if (instrument.Meter.Name == AppTelemetryFactory.InstrumentationName && ReferenceEquals(instrument.Meter.Scope, meterFactory))
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>(Record);
        _listener.SetMeasurementEventCallback<double>(Record);
        _listener.Start();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Stops listening for measurements.</summary>
    public void Dispose() => _listener.Dispose();

    private void Record<T>(Instrument instrument, T value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct =>
        logger.LogInformation("Metric {MetricName} = {MetricValue} {MetricTags}", instrument.Name, value, tags.ToArray());
}
