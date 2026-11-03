using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Polly;
using Polly.Retry;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

public abstract class ResilientWorkerBase(
    Observer observer,
    double recurringIntervalInSeconds = 2,
    double delayedIntervalInSeconds = 20,
    double performWorkTimeoutInSeconds = 120)
    : MonitoredWorkerBase(observer, recurringIntervalInSeconds, delayedIntervalInSeconds)
{
    private ResiliencePipeline? _policy;
    private readonly TimeSpan _performWorkTimeout = TimeSpan.FromSeconds(performWorkTimeoutInSeconds);

    protected abstract Task<bool> PerformWorkAsync(CancellationToken token);

    protected sealed override Task<bool> RunIterationAsync(CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();

        _policy ??= new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions {
                MaxRetryAttempts = int.MaxValue,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                DelayGenerator = args => new ValueTask<TimeSpan?>(GetRetrySleepDuration(args.AttemptNumber + 1)),
                OnRetry = args => {
                    var exception = args.Outcome.Exception;
                    if (exception is not null)
                        RecordFailureAction(caller, exception, args.AttemptNumber + 1, args.RetryDelay);

                    return default;
                }
            })
            .Build();

        return _policy.ExecuteAsync(async policyToken => {
            using var timeoutCts = new CancellationTokenSource(_performWorkTimeout);
            using var workCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, policyToken,
                timeoutCts.Token);

            try
            {
                return await PerformWorkAsync(workCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
            {
                throw new OperationTimeoutException(_performWorkTimeout);
            }
        }, stoppingToken).AsTask();
    }

    private TimeSpan GetRetrySleepDuration(int retryCount)
    {
        var quadraticDelayInSeconds = IntervalRecurring.TotalSeconds * Math.Pow(retryCount, 2);
        return TimeSpan.FromSeconds(Math.Min(quadraticDelayInSeconds, 120));
    }
}
