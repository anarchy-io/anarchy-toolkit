using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Application-facing telemetry, independent of any logging or metrics provider.</summary>
public interface IAppTelemetry
{
    /// <summary>Provides structured log-context scopes backed by this telemetry instance.</summary>
    LogContext LogContext => new(this);

    /// <summary>Begins a structured log scope whose lifetime is controlled by the caller.</summary>
    IDisposable BeginScope(IReadOnlyCollection<KeyValuePair<string, object?>> properties);

    /// <summary>Writes a structured log event with call-site attribution.</summary>
    void Log(LogLevel level, CallSite callSite, Exception? exception, string messageTemplate, params object?[] args);

    /// <summary>Increments a named counter using the supplied low-cardinality tags.</summary>
    void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>Records the current value of a named gauge.</summary>
    void Gauge(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>Records a sample in a named histogram.</summary>
    void Histogram(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>Records a text event as a structured log, rather than as a numeric metric.</summary>
    void RecordEvent(string title, string text, params KeyValuePair<string, object?>[] properties);

    /// <summary>Records a named duration in seconds using the supplied low-cardinality tags.</summary>
    void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags);

    /// <summary>Records the elapsed time as a duration when the returned timer is disposed.</summary>
    IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags);

    /// <summary>Runs an operation while recording its standardized execution count and duration.</summary>
    Task<TResult> MonitoredActionAsync<TResult>(CallSite callSite, Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);
}
