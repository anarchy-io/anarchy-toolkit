using Anarchy.Toolkit.Core.Telemetry;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Testing.Telemetry;

/// <summary>An <see cref="IMetricRecorder"/> that keeps every metric in memory, so tests can assert on them.</summary>
/// <remarks>Thread-safe. Optionally forwards every metric to another recorder.</remarks>
/// <param name="inner">A recorder every metric is forwarded to, or <see langword="null"/>.</param>
public sealed class InMemoryMetricRecorder(IMetricRecorder? inner = null) : IMetricRecorder
{
    private readonly ConcurrentQueue<RecordedMetric> _metrics = new();

    /// <summary>Every metric recorded so far, in order.</summary>
    public IReadOnlyList<RecordedMetric> Metrics => [.. _metrics];

    /// <summary>The metrics recorded with the given name.</summary>
    /// <param name="name">The metric name.</param>
    /// <returns>The matching metrics, in order.</returns>
    public IReadOnlyList<RecordedMetric> Named(string name) => [.. _metrics.Where(metric => metric.Name == name)];

    /// <summary>The sum of the counter's increments.</summary>
    /// <param name="name">The counter name.</param>
    /// <returns>The total; zero when it was never incremented.</returns>
    public double CounterTotal(string name) =>
        _metrics.Where(metric => metric.Kind == MetricKind.Counter && metric.Name == name).Sum(metric => metric.Value);

    /// <summary>Discards the recorded metrics.</summary>
    public void Clear() => _metrics.Clear();

    /// <inheritdoc />
    public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags)
    {
        Record(MetricKind.Counter, name, amount, tags);
        inner?.IncrementCounter(name, amount, tags);
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        Record(MetricKind.Gauge, name, value, tags);
        inner?.RecordGauge(name, value, tags);
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        Record(MetricKind.Histogram, name, value, tags);
        inner?.RecordHistogram(name, value, tags);
    }

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags)
    {
        Record(MetricKind.Duration, name, duration.TotalSeconds, tags);
        inner?.RecordDuration(name, duration, tags);
    }

    private void Record(MetricKind kind, string name, double value, KeyValuePair<string, object?>[] tags)
    {
        var tagValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags) tagValues[tag.Key] = tag.Value;
        _metrics.Enqueue(new RecordedMetric(kind, name, value, tagValues));
    }
}
