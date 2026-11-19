using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

/// <summary>
/// <see cref="WorkerBase"/> that records its lifecycle through <see cref="IAppTelemetry"/>: start, graceful stop,
/// fatal failure and a health ping after every iteration.
/// </summary>
/// <remarks>
/// Exceptions ending the loop are logged as fatal and counted (<c>failed</c> metric) but not rethrown, so the host keeps
/// running; <see cref="Finished"/> then becomes <see langword="true"/>. Each loop delay logs an information message
/// and increments the <c>health.ping</c> counter.
/// </remarks>
/// <param name="telemetry">Application telemetry used for logs and metrics.</param>
/// <param name="recurringIntervalInSeconds">Delay after an iteration that did work. Defaults to 2 seconds.</param>
/// <param name="delayedIntervalInSeconds">Delay after an idle iteration. Defaults to 20 seconds.</param>
public abstract class MonitoredWorkerBase(
    IAppTelemetry telemetry,
    double recurringIntervalInSeconds = 2,
    double delayedIntervalInSeconds = 20) : WorkerBase(recurringIntervalInSeconds, delayedIntervalInSeconds)
{
    /// <summary>Application telemetry the worker records its lifecycle with, available to derived workers.</summary>
    protected IAppTelemetry Telemetry { get; } = telemetry;

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

        Telemetry.Log(LogLevel.Information, caller, null, "Worker action ran, delaying next execution by {Delay}", delay);

        Telemetry.IncrementCounter($"{caller.ToMetricPrefix()}.health.ping", 1, [new("success", true)]);

        await base.LoopIntervalAsync(delay, stoppingToken);
    }

    /// <summary>
    /// Logs that the worker started.
    /// </summary>
    /// <param name="caller">The call site used for the log prefix.</param>
    /// <returns>The UTC start time.</returns>
    protected DateTime RecordStarted(CallSite caller)
    {
        var result = DateTime.UtcNow;
        Telemetry.Log(LogLevel.Information, caller, null, "Worker started at {@StartedAt}", result);
        return result;
    }

    /// <summary>
    /// Logs that the worker stopped gracefully, with its running time.
    /// </summary>
    /// <param name="caller">The call site used for the log prefix.</param>
    /// <param name="startedAt">The UTC start time returned by <see cref="RecordStarted"/>.</param>
    protected void RecordStopped(CallSite caller, DateTime startedAt)
    {
        var stoppedAt = DateTime.UtcNow;
        Telemetry.Log(
            LogLevel.Information,
            caller,
            null,
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
    protected void RecordFailure(CallSite caller, Exception exception, DateTime startedAt)
    {
        var stoppedAt = DateTime.UtcNow;
        Telemetry.Log(
            LogLevel.Critical,
            caller,
            exception,
            "Worker failed after running for {@ElapsedTime} and stopped at {@StoppedAt}",
            stoppedAt - startedAt,
            stoppedAt);

        Telemetry.IncrementCounter($"{caller.ToMetricPrefix()}.failed", 1, [new("success", false)]);
    }

    /// <summary>
    /// Logs a failed (retried) worker action as an error and increments the <c>failed.action</c> counter.
    /// </summary>
    /// <param name="caller">The call site used for the log prefix and metric name.</param>
    /// <param name="exception">The exception thrown by the action.</param>
    /// <param name="retryCount">The retry attempt number.</param>
    /// <param name="duration">The delay before the next attempt.</param>
    protected void RecordFailureAction(CallSite caller, Exception exception, int retryCount, TimeSpan duration)
    {
        Telemetry.Log(
            LogLevel.Error,
            caller,
            exception,
            "Worker action failed; retrying with {@RetryCount} attempts and {@RetryDelay} timeout",
            retryCount,
            duration);

        Telemetry.IncrementCounter($"{caller.ToMetricPrefix()}.failed.action", 1, [new("success", false)]);
    }
}
