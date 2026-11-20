using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;

/// <summary>
/// Decorates an <see cref="IMetricRecorder"/> so every metric is also written as a log event through the
/// application's logging pipeline, for hosts whose log backend derives metrics from logs.
/// </summary>
/// <remarks>
/// <para>Each metric is logged with the template <c>Metric {MetricType} {MetricName} = {MetricValue} {@MetricTags}</c>
/// (event id <see cref="MetricEventId"/>), where <c>MetricType</c> is <c>counter</c>, <c>gauge</c>,
/// <c>histogram</c> or <c>duration</c> (value in seconds); then it is forwarded to the decorated recorder.</para>
/// <para>Opt in with <see cref="SerilogTelemetryExtensions.AddTelemetryLogMetrics"/>. The decorated recorder and the
/// logger are not disposed here.</para>
/// </remarks>
/// <param name="inner">The decorated recorder.</param>
/// <param name="logger">The logger the metrics are written to.</param>
/// <param name="level">The level of the metric log events.</param>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates",
    Justification = "A single generic template; the level is configurable.")]
public sealed class LogMetricRecorder(IMetricRecorder inner, ILogger logger, LogLevel level = LogLevel.Information)
    : IMetricRecorder
{
    /// <summary>The logger category metric events are written to.</summary>
    public const string LoggerCategory = CoreTelemetry.InstrumentationName + ".Metrics";

    /// <summary>The event id of metric log events.</summary>
    public static readonly EventId MetricEventId = new(9100, "Metric");

    private const string Template = "Metric {MetricType} {MetricName} = {MetricValue} {@MetricTags}";

    private readonly IMetricRecorder _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags)
    {
        Write("counter", name, amount, tags);
        _inner.IncrementCounter(name, amount, tags);
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        Write("gauge", name, value, tags);
        _inner.RecordGauge(name, value, tags);
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        Write("histogram", name, value, tags);
        _inner.RecordHistogram(name, value, tags);
    }

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags)
    {
        Write("duration", name, duration.TotalSeconds, tags);
        _inner.RecordDuration(name, duration, tags);
    }

    private void Write(string type, string name, object value, KeyValuePair<string, object?>[] tags)
    {
        if (!_logger.IsEnabled(level)) return;
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags) properties[tag.Key] = tag.Value;
        _logger.Log(level, MetricEventId, Template, type, name, value, properties);
    }
}
