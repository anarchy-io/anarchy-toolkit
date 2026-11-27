using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Polly;
using Polly.Retry;

namespace Anarchy.Toolkit.Runtime.Workers.Service.Worker;

/// <summary>
/// <see cref="MonitoredWorkerBase"/> that retries failed work indefinitely with a per-attempt timeout.
/// </summary>
/// <remarks>
/// <para>Each iteration calls <see cref="PerformWorkAsync"/> through a Polly pipeline that retries any exception except
/// <see cref="OperationCanceledException"/>, without limit, waiting <c>recurringInterval × attempt²</c> seconds
/// (capped at 120 seconds) between attempts. Every retry is recorded with
/// <see cref="MonitoredWorkerBase.RecordFailureAction"/>.</para>
/// <para>Each attempt gets a token cancelled when the host stops or after <c>performWorkTimeoutInSeconds</c>; a
/// timeout surfaces as an <see cref="Anarchy.Toolkit.Core.Exceptions.OperationTimeoutException"/>, which is retried.</para>
/// <para>A polling iteration has no inbound ids, so each iteration gets a new correlation id and each attempt a new
/// attempt id (see <see cref="MonitoredWorkerBase.BeginCorrelationScope"/>); logs written by the work carry both, and
/// the retry logs written between attempts carry the correlation id. Work that receives its own ids (for example from a message) opens a nested
/// <see cref="MonitoredWorkerBase.BeginCorrelationScope"/> with them.</para>
/// </remarks>
/// <param name="telemetry">Application telemetry used for logs and metrics.</param>
/// <param name="recurringIntervalInSeconds">Delay after an iteration that did work, also the retry back-off base. Defaults to 2 seconds.</param>
/// <param name="delayedIntervalInSeconds">Delay after an idle iteration. Defaults to 20 seconds.</param>
/// <param name="performWorkTimeoutInSeconds">Timeout of a single <see cref="PerformWorkAsync"/> attempt. Defaults to 120 seconds.</param>
/// <param name="correlationIdOptions">Names the correlation log properties; the defaults when <see langword="null"/>.</param>
/// <example>
/// <code language="csharp">
/// public sealed class OrderSyncWorker(AppTelemetry telemetry, IOrderSync sync) : ResilientWorkerBase(telemetry)
/// {
///     protected override async Task&lt;bool&gt; PerformWorkAsync(CancellationToken token) =>
///         await sync.SyncNextBatchAsync(token) &gt; 0;
/// }
/// </code>
/// </example>
public abstract class ResilientWorkerBase(
    AppTelemetry telemetry,
    double recurringIntervalInSeconds = 2,
    double delayedIntervalInSeconds = 20,
    double performWorkTimeoutInSeconds = 120,
    CorrelationIdOptions? correlationIdOptions = null)
    : MonitoredWorkerBase(telemetry, recurringIntervalInSeconds, delayedIntervalInSeconds, correlationIdOptions)
{
    private ResiliencePipeline? _policy;
    private readonly TimeSpan _performWorkTimeout = TimeSpan.FromSeconds(performWorkTimeoutInSeconds);

    /// <summary>
    /// Performs one unit of work. Throw to have the attempt retried.
    /// </summary>
    /// <param name="token">Cancelled when the host stops or the attempt times out.</param>
    /// <returns>
    /// <see langword="true"/> if work was done; <see langword="false"/> when idle (the next iteration is delayed
    /// longer).
    /// </returns>
    protected abstract Task<bool> PerformWorkAsync(CancellationToken token);

    /// <inheritdoc />
    protected sealed override async Task<bool> RunIterationAsync(CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();
        var correlationId = CreateCorrelationId();

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

        // Between attempts (where the retry is logged) only the correlation id is set.
        using var iterationScope = CorrelationContext.BeginScope(Telemetry.Logger, correlationId, attemptId: null,
            CorrelationIdOptions.CorrelationIdPropertyName, CorrelationIdOptions.AttemptIdPropertyName);

        return await _policy.ExecuteAsync(async policyToken => {
            using var attemptScope = BeginCorrelationScope(correlationId);
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
        }, stoppingToken);
    }

    private TimeSpan GetRetrySleepDuration(int retryCount)
    {
        var quadraticDelayInSeconds = IntervalRecurring.TotalSeconds * Math.Pow(retryCount, 2);
        return TimeSpan.FromSeconds(Math.Min(quadraticDelayInSeconds, 120));
    }
}
