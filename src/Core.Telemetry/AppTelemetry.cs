using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Composes Microsoft logging and standard .NET metric instruments.</summary>
/// <remarks>The supplied logger and meter are externally owned and are not disposed by this class.</remarks>
public sealed class AppTelemetry(ILogger logger, Meter meter) : IAppTelemetry
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Meter _meter = meter ?? throw new ArgumentNullException(nameof(meter));
    private readonly ConcurrentDictionary<string, Lazy<Counter<long>>> _counters = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Gauge<double>>> _gauges = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Histogram<double>>> _histograms = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Histogram<double>>> _durations = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public LogContext LogContext => new(this);

    /// <inheritdoc />
    public IDisposable BeginScope(IReadOnlyCollection<KeyValuePair<string, object?>> properties) =>
        _logger.BeginScope(properties) ?? EmptyScope.Instance;

    /// <inheritdoc />
    public void Log(LogLevel level, CallSite callSite, Exception? exception, string messageTemplate, params object?[] args) =>
        _logger.Log(level, exception, "{CallSite} " + messageTemplate, [callSite.ToLogPrefix(), .. args]);

    /// <inheritdoc />
    public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags) =>
        _counters.GetOrAdd(name, metric => new Lazy<Counter<long>>(() => _meter.CreateCounter<long>(metric))).Value.Add(amount, tags);

    /// <inheritdoc />
    public void Gauge(string name, double value, params KeyValuePair<string, object?>[] tags) =>
        _gauges.GetOrAdd(name, metric => new Lazy<Gauge<double>>(() => _meter.CreateGauge<double>(metric))).Value.Record(value, tags);

    /// <inheritdoc />
    public void Histogram(string name, double value, params KeyValuePair<string, object?>[] tags) =>
        _histograms.GetOrAdd(name, metric => new Lazy<Histogram<double>>(() => _meter.CreateHistogram<double>(metric))).Value.Record(value, tags);

    /// <inheritdoc />
    public void RecordEvent(string title, string text, params KeyValuePair<string, object?>[] properties)
    {
        using var scope = BeginScope(properties);
        _logger.LogInformation("Telemetry event {EventTitle}: {EventText}", title, text);
    }

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags) =>
        _durations.GetOrAdd(name, metric => new Lazy<Histogram<double>>(() => _meter.CreateHistogram<double>(metric, "s"))).Value.Record(duration.TotalSeconds, tags);

    /// <inheritdoc />
    public IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags) =>
        new MetricTimer(this, name, [.. tags]);

    /// <inheritdoc />
    public async Task<TResult> MonitoredActionAsync<TResult>(CallSite callSite, Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();
        var success = false;
        var cancelled = false;
        try
        {
            var result = await action();
            success = true;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
            Log(LogLevel.Information, callSite, null, "Service action cancelled");
            throw;
        }
        catch (Exception exception)
        {
            Log(LogLevel.Error, callSite, exception, "Service action failed");
            throw;
        }
        finally
        {
            KeyValuePair<string, object?>[] tags = cancelled
                ? [new("success", false), new("cancelled", true)]
                : [new("success", success)];
            var prefix = callSite.ToMetricPrefix();
            RecordDuration($"{prefix}.execution_time", Stopwatch.GetElapsedTime(started), tags);
            IncrementCounter($"{prefix}.execution_count", 1, tags);
        }
    }

    private sealed class EmptyScope : IDisposable
    {
        public static EmptyScope Instance { get; } = new();
        public void Dispose() { }
    }
}
