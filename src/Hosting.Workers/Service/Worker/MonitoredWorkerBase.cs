using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

public abstract class MonitoredWorkerBase(
    Observer observer,
    double recurringIntervalInSeconds = 2,
    double delayedIntervalInSeconds = 20) : WorkerBase(recurringIntervalInSeconds, delayedIntervalInSeconds)
{
    /// <summary>The observer the worker records its lifecycle with, available to derived workers.</summary>
    protected Observer Observer { get; } = observer;

    public bool Finished { get; private set; }

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

    protected override async Task LoopIntervalAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();

        Observer.LogInformation((CallSiteEntity)caller, "Worker action ran, delaying next execution by {Delay}", delay);

        Observer.IncrementCounter(caller, Observer.MetricSuffixHealthPing, 1, Tags.Success);

        await base.LoopIntervalAsync(delay, stoppingToken);
    }

    protected DateTime RecordStarted(CallSiteEntity caller)
    {
        var result = DateTime.UtcNow;
        Observer.LogInformation(caller, "Worker started at {@StartedAt}", result);
        return result;
    }

    protected void RecordStopped(CallSiteEntity caller, DateTime startedAt)
    {
        var stoppedAt = DateTime.UtcNow;
        Observer.LogInformation(
            caller,
            "Worker ran for {@ElapsedTime} and gracefully stopped at {@StoppedAt}",
            stoppedAt - startedAt,
            stoppedAt);
    }

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
