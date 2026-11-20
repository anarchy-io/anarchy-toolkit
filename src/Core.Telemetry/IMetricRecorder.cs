namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>A small, provider-neutral contract for recording metrics.</summary>
/// <remarks>
/// <para>The default implementation, <see cref="MeterMetricRecorder"/>, records standard
/// <see cref="System.Diagnostics.Metrics"/> instruments that OpenTelemetry (and so OTLP) collects without custom code.
/// Register another implementation, or a decorator around the default one, to send metrics elsewhere.</para>
/// <para>Keep tags low-cardinality: every distinct tag combination becomes its own time series in most backends.</para>
/// </remarks>
public interface IMetricRecorder
{
    /// <summary>Adds <paramref name="amount"/> to a monotonic counter.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="amount">The non-negative amount to add.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>Records the current value of a gauge, such as a queue depth.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The current value.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>Records a sample in a histogram, such as a payload size.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The sampled value.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>Records a duration, in seconds, in a histogram.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="duration">The measured duration.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags);
}
