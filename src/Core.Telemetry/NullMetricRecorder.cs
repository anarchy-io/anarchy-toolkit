namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>A recorder that discards every metric, for tools and tests that do not collect metrics.</summary>
public sealed class NullMetricRecorder : IMetricRecorder
{
    /// <summary>The shared instance.</summary>
    public static NullMetricRecorder Instance { get; } = new();

    private NullMetricRecorder() { }

    /// <inheritdoc />
    public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags) { }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags) { }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags) { }

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags) { }
}
