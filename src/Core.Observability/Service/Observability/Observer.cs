using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Serilog;
using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// Single entry point for observability: structured logging through <see cref="Logger"/>, metrics through
/// <see cref="MeasurerBase"/> and ambient log properties through <see cref="LogContext"/>.
/// The observer owns its <see cref="Logger"/>: disposing the observer flushes and disposes it.
/// </summary>
/// <remarks>
/// <para>Logging helpers (<c>LogDebug</c> … <c>LogFatal</c>) prefix messages with a <see cref="CallSiteEntity"/> log
/// prefix; metric helpers (<see cref="ExecutionRecordDuration(CallSiteEntity, TimeSpan, string[])"/>,
/// <see cref="IncrementCounter(CallSiteEntity, string, int, string[])"/>, …) derive metric names from the call site's
/// metric prefix plus one of the <c>MetricSuffix*</c> constants.</para>
/// <para>In hosted applications the observer is normally created and registered by the Configuration package
/// (<c>ModuleObservability</c>/<c>ObserverFactory</c>) and injected via DI.</para>
/// </remarks>
/// <param name="logger">The Serilog logger. Owned by the observer and disposed with it.</param>
/// <param name="measurer">The metrics implementation.</param>
/// <exception cref="ArgumentNullException">
/// Thrown if <paramref name="logger"/> or <paramref name="measurer"/> is <see langword="null"/>.
/// </exception>
/// <example>
/// <code language="csharp">
/// var callSite = this.CalledFrom();
/// var order = await observer.MonitoredActionAsync(callSite, () => repository.GetAsync(id));
/// observer.LogInformation(callSite, "Loaded order {OrderId}", order.Id);
/// </code>
/// </example>
public partial class Observer(ILogger logger, MeasurerBase measurer) : IDisposable
{
    /// <summary>
    /// The configured logger instance.
    /// </summary>
    public ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// The configured metrics implementation.
    /// </summary>
    public MeasurerBase MeasurerBase { get; } = measurer ?? throw new ArgumentNullException(nameof(measurer));

    /// <summary>
    /// Pushes ambient properties onto every log event written while the returned scope is open.
    /// </summary>
    public LogContext LogContext { get; } = new();

    /// <summary>
    /// Runs <paramref name="action"/>, recording its duration and execution count and logging failures.
    /// </summary>
    /// <remarks>
    /// <para>Metrics are recorded as <c>{prefix}.execution_time</c> and <c>{prefix}.execution_count</c>, tagged with
    /// <c>success:true</c> or <c>success:false</c>. If the action throws an <see cref="OperationCanceledException"/>
    /// while <paramref name="cancellationToken"/> is cancelled, an information message is logged and the metrics get an
    /// extra <c>cancelled:true</c> tag. Any other exception is logged as an error (or warning) with the message
    /// "Service action failed".</para>
    /// <para>Exceptions are always rethrown.</para>
    /// </remarks>
    /// <typeparam name="TResult">The action's result type.</typeparam>
    /// <param name="caller">The call site used for log prefixes and metric names.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="recordExecutionDuration">Whether to record the <c>execution_time</c> timer.</param>
    /// <param name="recordExecutionCount">Whether to increment the <c>execution_count</c> counter.</param>
    /// <param name="logErrorAsWarning">Log failures at warning instead of error level.</param>
    /// <param name="cancellationToken">
    /// Optional token used only to distinguish cancellation from failure; it is not passed to
    /// <paramref name="action"/>.
    /// </param>
    /// <returns>The result of <paramref name="action"/>.</returns>
    public async Task<TResult> MonitoredActionAsync<TResult>(
        CallSiteEntity caller,
        Func<Task<TResult>> action,
        bool recordExecutionDuration = true,
        bool recordExecutionCount = true,
        bool logErrorAsWarning = false,
        CancellationToken? cancellationToken = null)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            var result = await action();

            if (recordExecutionDuration) ExecutionRecordDuration(caller, timer.Elapsed, Tags.Success);
            if (recordExecutionCount) ExecutionIncrementCount(caller, 1, Tags.Success);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken?.IsCancellationRequested == true)
        {
            LogInformation(caller, "Service action cancelled");

            var cancelledTags = Tags.Failure.WithTag("cancelled", "true");
            if (recordExecutionDuration) ExecutionRecordDuration(caller, timer.Elapsed, cancelledTags);
            if (recordExecutionCount) ExecutionIncrementCount(caller, 1, cancelledTags);

            throw;
        }
        catch (Exception exception)
        {
            if (logErrorAsWarning)
            {
                LogWarning(exception, caller, "Service action failed");
            }
            else
            {
                LogError(exception, caller, "Service action failed");
            }

            if (recordExecutionDuration) ExecutionRecordDuration(caller, timer.Elapsed, Tags.Failure);
            if (recordExecutionCount) ExecutionIncrementCount(caller, 1, Tags.Failure);

            throw;
        }
    }

    /// <summary>
    /// Flushes and disposes the <see cref="Logger"/>, so buffered sinks (e.g. CloudWatch) deliver pending events.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources held by the observer.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when called from <see cref="Dispose()"/>; disposes <see cref="Logger"/> if it is
    /// <see cref="IDisposable"/>.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            (Logger as IDisposable)?.Dispose();
        }
    }
}
