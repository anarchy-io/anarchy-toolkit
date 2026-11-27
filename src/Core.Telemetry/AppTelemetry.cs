using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>
/// The toolkit's telemetry conventions on top of the <see cref="ICoreTelemetry"/> primitives: call-site attribution,
/// standard metric names, timers, monitored actions and log helpers.
/// </summary>
/// <remarks>
/// <para>Toolkit packages and applications depend on this class; it is registered by
/// <see cref="TelemetryServiceCollectionExtensions.AddCoreTelemetry"/>. To change where logs and metrics go, replace
/// <see cref="ICoreTelemetry"/>, <see cref="IMetricRecorder"/> or the logging providers, not this class.</para>
/// <para>Notable occurrences are logged with a stable <see cref="EventId"/>
/// (<see cref="Log(LogLevel, CallSite, EventId, Exception?, string, object?[])"/>) and, where the count matters,
/// counted with <see cref="IncrementCounter(CallSite, string, long, KeyValuePair{string, object?}[])"/>. Routing them
/// to a vendor's event API is a logging provider's or recorder's job.</para>
/// <para>Nothing is disposed here: the primitives belong to whoever created them.</para>
/// </remarks>
/// <param name="core">The logger and metric recorder.</param>
/// <example>
/// <code language="csharp">
/// public sealed class OrderService(AppTelemetry telemetry)
/// {
///     public Task&lt;Order&gt; LoadAsync(int id, CancellationToken token) =>
///         telemetry.MonitoredActionAsync(this.CalledFrom(), () => repository.GetAsync(id, token), token);
/// }
/// </code>
/// </example>
[SuppressMessage("Usage", "CA2254:Template should be a static expression",
    Justification = "Templates are supplied by the callers and prefixed with the call site.")]
public sealed class AppTelemetry(ICoreTelemetry core)
{
    /// <summary>Discards every log and metric, for tests and tools that do not observe telemetry.</summary>
    public static AppTelemetry Null { get; } = new(CoreTelemetry.Null);

    /// <summary>The primitives this instance builds on.</summary>
    public ICoreTelemetry Core { get; } = core ?? throw new ArgumentNullException(nameof(core));

    /// <summary>The logger.</summary>
    public ILogger Logger => Core.Logger;

    /// <summary>The metric recorder.</summary>
    public IMetricRecorder Metrics => Core.Metrics;

    /// <summary>Opens structured log scopes on <see cref="Logger"/>.</summary>
    public LogContext LogContext => _logContext ??= new LogContext(Core.Logger);

    private LogContext? _logContext;

    /// <summary>
    /// Creates the telemetry from existing logging and metrics infrastructure, without dependency injection (for
    /// console tools and tests).
    /// </summary>
    /// <param name="loggerFactory">Creates the logger; owned by the caller.</param>
    /// <param name="meterFactory">Creates and owns the meter; metrics are discarded when <see langword="null"/>.</param>
    /// <returns>The telemetry.</returns>
    public static AppTelemetry Create(ILoggerFactory loggerFactory, IMeterFactory? meterFactory = null) =>
        new(CoreTelemetry.Create(loggerFactory, meterFactory));

    /// <summary>Returns the standard metric name <c>{class_name}.{method_name}.{suffix}</c>.</summary>
    /// <param name="callSite">The operation's call site.</param>
    /// <param name="suffix">The suffix, usually one of <see cref="MetricSuffixes"/>.</param>
    /// <returns>The metric name.</returns>
    public static string MetricName(CallSite callSite, string suffix) => $"{callSite.ToMetricPrefix()}.{suffix}";

    #region Logs

    /// <summary>Writes a log event prefixed with the call site.</summary>
    /// <param name="level">The level.</param>
    /// <param name="callSite">The call site, rendered as <c>[Class → Method]</c>.</param>
    /// <param name="exception">The exception associated with the event, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void Log(LogLevel level, CallSite callSite, Exception? exception, string messageTemplate, params object?[] args) =>
        Log(level, callSite, default, exception, messageTemplate, args);

    /// <summary>Writes a log event with a stable event id, prefixed with the call site.</summary>
    /// <param name="level">The level.</param>
    /// <param name="callSite">The call site, rendered as <c>[Class → Method]</c>.</param>
    /// <param name="eventId">The stable id and name identifying this kind of event.</param>
    /// <param name="exception">The exception associated with the event, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void Log(LogLevel level, CallSite callSite, EventId eventId, Exception? exception, string messageTemplate,
                    params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(callSite);
        if (!Logger.IsEnabled(level)) return;
        Logger.Log(level, eventId, exception, "{CallSite} " + messageTemplate, [callSite.ToLogPrefix(), .. args]);
    }

    /// <summary>Writes a <see cref="LogLevel.Debug"/> event prefixed with the call site.</summary>
    /// <param name="callSite">The call site.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void LogDebug(CallSite callSite, string messageTemplate, params object?[] args) =>
        Log(LogLevel.Debug, callSite, null, messageTemplate, args);

    /// <summary>Writes a <see cref="LogLevel.Information"/> event prefixed with the call site.</summary>
    /// <param name="callSite">The call site.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void LogInformation(CallSite callSite, string messageTemplate, params object?[] args) =>
        Log(LogLevel.Information, callSite, null, messageTemplate, args);

    /// <summary>Writes a <see cref="LogLevel.Warning"/> event prefixed with the call site.</summary>
    /// <param name="callSite">The call site.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void LogWarning(CallSite callSite, string messageTemplate, params object?[] args) =>
        Log(LogLevel.Warning, callSite, null, messageTemplate, args);

    /// <summary>Writes a <see cref="LogLevel.Warning"/> event with an exception, prefixed with the call site.</summary>
    /// <param name="callSite">The call site.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void LogWarning(CallSite callSite, Exception? exception, string messageTemplate, params object?[] args) =>
        Log(LogLevel.Warning, callSite, exception, messageTemplate, args);

    /// <summary>Writes a <see cref="LogLevel.Error"/> event prefixed with the call site.</summary>
    /// <param name="callSite">The call site.</param>
    /// <param name="exception">The exception, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void LogError(CallSite callSite, Exception? exception, string messageTemplate, params object?[] args) =>
        Log(LogLevel.Error, callSite, exception, messageTemplate, args);

    /// <summary>Writes a <see cref="LogLevel.Critical"/> event prefixed with the call site.</summary>
    /// <param name="callSite">The call site.</param>
    /// <param name="exception">The exception, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The template's values.</param>
    public void LogCritical(CallSite callSite, Exception? exception, string messageTemplate, params object?[] args) =>
        Log(LogLevel.Critical, callSite, exception, messageTemplate, args);

    #endregion

    #region Metrics

    /// <summary>Adds to a counter.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="amount">The amount to add.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags) =>
        Metrics.IncrementCounter(name, amount, tags);

    /// <summary>Adds to the counter <c>{class_name}.{method_name}.{suffix}</c>.</summary>
    /// <param name="callSite">The call site naming the metric.</param>
    /// <param name="suffix">The suffix, usually one of <see cref="MetricSuffixes"/>.</param>
    /// <param name="amount">The amount to add.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void IncrementCounter(CallSite callSite, string suffix, long amount = 1, params KeyValuePair<string, object?>[] tags) =>
        Metrics.IncrementCounter(MetricName(callSite, suffix), amount, tags);

    /// <summary>Records the current value of a gauge.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The current value.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags) =>
        Metrics.RecordGauge(name, value, tags);

    /// <summary>Records the current value of the gauge <c>{class_name}.{method_name}.{suffix}</c>.</summary>
    /// <param name="callSite">The call site naming the metric.</param>
    /// <param name="suffix">The metric suffix.</param>
    /// <param name="value">The current value.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void RecordGauge(CallSite callSite, string suffix, double value, params KeyValuePair<string, object?>[] tags) =>
        Metrics.RecordGauge(MetricName(callSite, suffix), value, tags);

    /// <summary>Records a histogram sample.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The sampled value.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags) =>
        Metrics.RecordHistogram(name, value, tags);

    /// <summary>Records a sample in the histogram <c>{class_name}.{method_name}.{suffix}</c>.</summary>
    /// <param name="callSite">The call site naming the metric.</param>
    /// <param name="suffix">The metric suffix.</param>
    /// <param name="value">The sampled value.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void RecordHistogram(CallSite callSite, string suffix, double value, params KeyValuePair<string, object?>[] tags) =>
        Metrics.RecordHistogram(MetricName(callSite, suffix), value, tags);

    /// <summary>Records a duration, in seconds.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="duration">The measured duration.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags) =>
        Metrics.RecordDuration(name, duration, tags);

    /// <summary>Records a duration, in seconds, as <c>{class_name}.{method_name}.execution_time</c>.</summary>
    /// <param name="callSite">The call site naming the metric.</param>
    /// <param name="duration">The measured duration.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    public void RecordDuration(CallSite callSite, TimeSpan duration, params KeyValuePair<string, object?>[] tags) =>
        Metrics.RecordDuration(MetricName(callSite, MetricSuffixes.ExecutionTime), duration, tags);

    /// <summary>Starts a timer that records its duration when disposed.</summary>
    /// <param name="name">The metric name.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    /// <returns>The running timer.</returns>
    public MetricTimer StartTimer(string name, params KeyValuePair<string, object?>[] tags) =>
        new(Metrics, name, [.. tags]);

    /// <summary>
    /// Starts a timer that records its duration as <c>{class_name}.{method_name}.execution_time</c> when disposed.
    /// </summary>
    /// <param name="callSite">The call site naming the metric.</param>
    /// <param name="tags">Low-cardinality tags.</param>
    /// <returns>The running timer.</returns>
    public MetricTimer StartTimer(CallSite callSite, params KeyValuePair<string, object?>[] tags) =>
        new(Metrics, MetricName(callSite, MetricSuffixes.ExecutionTime), [.. tags]);

    #endregion

    #region Monitored actions

    /// <summary>
    /// Runs an operation, recording <c>{prefix}.execution_time</c> and <c>{prefix}.execution_count</c> tagged with
    /// <c>success</c> (and <c>cancelled</c> on cancellation) and logging failures.
    /// </summary>
    /// <remarks>
    /// A cancellation through <paramref name="cancellationToken"/> is logged at information level; other exceptions at
    /// error level. Exceptions are always rethrown. The token is not passed to <paramref name="action"/>.
    /// </remarks>
    /// <typeparam name="TResult">The operation's result.</typeparam>
    /// <param name="callSite">The call site naming the metrics and prefixing the logs.</param>
    /// <param name="action">The operation.</param>
    /// <param name="cancellationToken">Distinguishes a cancellation from a failure.</param>
    /// <returns>The operation's result.</returns>
    public async Task<TResult> MonitoredActionAsync<TResult>(CallSite callSite, Func<Task<TResult>> action,
                                                             CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
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
            LogInformation(callSite, "Service action cancelled");
            throw;
        }
        catch (Exception exception)
        {
            LogError(callSite, exception, "Service action failed");
            throw;
        }
        finally
        {
            KeyValuePair<string, object?>[] tags = cancelled
                ? [new("success", false), new("cancelled", true)]
                : [new("success", success)];
            RecordDuration(callSite, Stopwatch.GetElapsedTime(started), tags);
            IncrementCounter(callSite, MetricSuffixes.ExecutionCount, 1, tags);
        }
    }

    /// <inheritdoc cref="MonitoredActionAsync{TResult}(CallSite, Func{Task{TResult}}, CancellationToken)"/>
    public Task MonitoredActionAsync(CallSite callSite, Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return MonitoredActionAsync(callSite, async () => {
            await action();
            return true;
        }, cancellationToken);
    }

    #endregion
}
