namespace Anarchy.Toolkit.Testing.Telemetry;

/// <summary>The kind of a <see cref="RecordedMetric"/>, matching the <see cref="Core.Telemetry.IMetricRecorder"/> operations.</summary>
public enum MetricKind
{
    /// <summary><see cref="Core.Telemetry.IMetricRecorder.IncrementCounter"/>.</summary>
    Counter,

    /// <summary><see cref="Core.Telemetry.IMetricRecorder.RecordGauge"/>.</summary>
    Gauge,

    /// <summary><see cref="Core.Telemetry.IMetricRecorder.RecordHistogram"/>.</summary>
    Histogram,

    /// <summary><see cref="Core.Telemetry.IMetricRecorder.RecordDuration"/>; the value is in seconds.</summary>
    Duration
}

/// <summary>A metric captured by <see cref="InMemoryMetricRecorder"/>.</summary>
/// <param name="Kind">The kind of metric.</param>
/// <param name="Name">The metric name.</param>
/// <param name="Value">The value; durations are in seconds.</param>
/// <param name="Tags">The tags; when a tag is repeated, the last value wins.</param>
public sealed record RecordedMetric(MetricKind Kind, string Name, double Value, IReadOnlyDictionary<string, object?> Tags)
{
    /// <summary>Whether the metric carries the tag with the given value.</summary>
    /// <param name="name">The tag name.</param>
    /// <param name="value">The expected value, compared with <see cref="object.Equals(object, object)"/>.</param>
    /// <returns><see langword="true"/> when the tag is present with that value.</returns>
    public bool HasTag(string name, object? value) => Tags.TryGetValue(name, out var actual) && Equals(actual, value);
}
