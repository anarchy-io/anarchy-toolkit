using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

/// <summary>
/// <see cref="WorkerBase"/> that records its lifecycle through an <see cref="Observer"/>: start, graceful stop,
/// fatal failure and a health ping after every iteration.
/// </summary>
/// <remarks>
/// Exceptions ending the loop are logged as fatal and counted (<c>failed</c> metric) but not rethrown, so the host keeps
/// running; <see cref="Finished"/> then becomes <see langword="true"/>. Each loop delay logs an information message
/// and increments the <c>health.ping</c> counter.
/// </remarks>
/// <param name="observer">The observer used for logs and metrics.</param>
/// <param name="recurringIntervalInSeconds">Delay after an iteration that did work. Defaults to 2 seconds.</param>
/// <param name="delayedIntervalInSeconds">Delay after an idle iteration. Defaults to 20 seconds.</param>
public abstract class MonitoredWorkerBase(
    Observer observer,
    double recurringIntervalInSeconds = 2,
    double delayedIntervalInSeconds = 20) : WorkerBase(recurringIntervalInSeconds, delayedIntervalInSeconds)
{
    /// <summary>The observer the worker records its lifecycle with, available to derived workers.</summary>
    protected Observer Observer { get; } = observer;

    /// <summary>
    /// Gets whether the worker loop has ended (stopped or failed).
    /// </summary>
    public bool Finished { get; private set; }

    /// <inheritdoc />
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();
        var startedAt = RecordStarted(caller);

        try
        {
            await base.ExecuteAsync(stoppingToken);
            RecordStopped(caller, startedAt);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                RecordStopped(caller, startedAt);
            else
                RecordFailure(caller, exception, startedAt);
        }

        Finished = true;
    }

    /// <inheritdoc />
    protected override async Task LoopIntervalAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();

        Observer.LogInformation((CallSiteEntity)caller, "Worker action ran, delaying next execution by {Delay}", delay);

        Observer.IncrementCounter(caller, Observer.MetricSuffixHealthPing, 1, Tags.Success);

        await base.LoopIntervalAsync(delay, stoppingToken);
    }

    /// <summary>
    /// Logs that the worker started.
    /// </summary>
    /// <param name="caller">The call site used for the log prefix.</param>
    /// <returns>The UTC start time.</returns>
    protected DateTime RecordStarted(CallSiteEntity caller)
    {
        var result = DateTime.UtcNow;
        Observer.LogInformation(caller, "Worker started at {@StartedAt}", result);
        return result;
    }

    /// <summary>
    /// Logs that the worker stopped gracefully, with its running time.
    /// </summary>
    /// <param name="caller">The call site used for the log prefix.</param>
    /// <param name="startedAt">The UTC start time returned by <see cref="RecordStarted"/>.</param>
    protected void RecordStopped(CallSiteEntity caller, DateTime startedAt)
    {
        var stoppedAt = DateTime.UtcNow;
        Observer.LogInformation(
            caller,
            "Worker ran for {@ElapsedTime} and gracefully stopped at {@StoppedAt}",
            stoppedAt - startedAt,
            stoppedAt);
    }

    /// <summary>
    /// Logs a fatal worker failure and increments the <c>failed</c> counter.
    /// </summary>
    /// <param name="caller">The call site used for the log prefix and metric name.</param>
    /// <param name="exception">The exception that ended the worker.</param>
    /// <param name="startedAt">The UTC start time returned by <see cref="RecordStarted"/>.</param>
    protected void RecordFailure(CallSiteEntity caller, Exception exception, DateTime startedAt)
    {
        var stoppedAt = DateTime.UtcNow;
        Observer.LogFatal(
            exception,
            caller,
            "Worker failed after running for {@ElapsedTime} and stopped at {@StoppedAt}",
            stoppedAt - startedAt,
            stoppedAt);

        Observer.IncrementCounter(caller, Observer.MetricSuffixFailed, 1, Tags.Failure);
    }

    /// <summary>
    /// Logs a failed (retried) worker action as an error and increments the <c>failed.action</c> counter.
    /// </summary>
    /// <param name="caller">The call site used for the log prefix and metric name.</param>
    /// <param name="exception">The exception thrown by the action.</param>
    /// <param name="retryCount">The retry attempt number.</param>
    /// <param name="duration">The delay before the next attempt.</param>
    protected void RecordFailureAction(CallSiteEntity caller, Exception exception, int retryCount, TimeSpan duration)
    {
        Observer.LogError(
            exception,
            caller,
            "Worker action failed; retrying with {@RetryCount} attempts and {@RetryDelay} timeout",
            retryCount,
            duration);

        Observer.IncrementCounter(caller, Observer.MetricSuffixFailedAction, 1, Tags.Failure);
    }
}
