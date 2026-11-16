using Serilog;
using Serilog.Events;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// Vendor-neutral <see cref="MeasurerBase"/> that writes every metric as a structured log event, so metrics are not lost
/// before a dedicated metrics backend (e.g. DogStatsD) is plugged in. Ambient <see cref="MeasurerBase.BeginScope"/> tags
/// are included.
/// </summary>
/// <remarks>
/// Each metric is written with the template <c>Metric {MetricType} {MetricName} = {MetricValue} {@MetricTags}</c>,
/// where <c>MetricType</c> is one of <c>counter</c>, <c>gauge</c>, <c>histogram</c>, <c>timer</c> (value in
/// milliseconds) or <c>event</c>.
/// </remarks>
/// <param name="logger">The logger metrics are written to.</param>
/// <param name="level">The level of the emitted log events. Defaults to <see cref="LogEventLevel.Information"/>.</param>
/// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <see langword="null"/>.</exception>
public sealed class LoggerMeasurer(ILogger logger, LogEventLevel level = LogEventLevel.Information) : MeasurerBase
{
    private const string Template = "Metric {MetricType} {MetricName} = {MetricValue} {@MetricTags}";

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public override void IncrementCounter(string name, params string[] tags) => IncrementCounter(name, 1, tags);

    /// <inheritdoc />
    public override void IncrementCounter(string name, int amount, params string[] tags) =>
        Write("counter", name, amount, tags);

    /// <inheritdoc />
    public override void Gauge(string name, double value, params string[] tags) => Write("gauge", name, value, tags);

    /// <inheritdoc />
    public override void Histogram(string name, double value, params string[] tags) =>
        Write("histogram", name, value, tags);

    /// <inheritdoc />
    public override void Timer(string name, TimeSpan elapsedTime, params string[] tags) =>
        Write("timer", name, elapsedTime.TotalMilliseconds, tags);

    /// <inheritdoc />
    public override void Event(string title, string text, params string[] tags) => Write("event", title, text, tags);

    private void Write(string type, string name, object value, string[] tags) =>
        _logger.Write(level, Template, type, name, value, GetTags(tags));
}
