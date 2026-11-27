using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Runtime.Workers.Service.Worker;

/// <summary>
/// Base class for polling background workers: runs <see cref="RunIterationAsync"/> in a loop until the host stops.
/// </summary>
/// <remarks>
/// After each iteration the worker waits <see cref="IntervalRecurring"/> when the iteration did some work (returned
/// <see langword="true"/>), or <see cref="IntervalDelayed"/> when it was idle, so busy workers drain quickly and idle
/// ones back off. Exceptions from an iteration end the loop. See <see cref="MonitoredWorkerBase"/> and
/// <see cref="ResilientWorkerBase"/> for logging and retries.
/// </remarks>
/// <param name="recurringIntervalInSeconds">Delay after an iteration that did work. Defaults to 2 seconds.</param>
/// <param name="delayedIntervalInSeconds">Delay after an idle iteration. Defaults to 20 seconds.</param>
public abstract class WorkerBase(
    double recurringIntervalInSeconds = 2,
    double delayedIntervalInSeconds = 20) : BackgroundService
{
    /// <summary>The delay applied after an iteration that did some work.</summary>
    protected readonly TimeSpan IntervalRecurring = TimeSpan.FromSeconds(recurringIntervalInSeconds);
    /// <summary>The delay applied after an idle iteration.</summary>
    protected readonly TimeSpan IntervalDelayed = TimeSpan.FromSeconds(delayedIntervalInSeconds);

    /// <summary>
    /// Performs one unit of work.
    /// </summary>
    /// <param name="stoppingToken">Signaled when the host is stopping.</param>
    /// <returns>
    /// <see langword="true"/> if work was done (next iteration after <see cref="IntervalRecurring"/>); otherwise
    /// <see langword="false"/> (next iteration after <see cref="IntervalDelayed"/>).
    /// </returns>
    protected abstract Task<bool> RunIterationAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Waits between iterations. Override to add behavior (e.g. logging) around the delay.
    /// </summary>
    /// <param name="delay">The delay to wait.</param>
    /// <param name="stoppingToken">Signaled when the host is stopping.</param>
    /// <returns>A task that completes after the delay.</returns>
    protected virtual Task LoopIntervalAsync(TimeSpan delay, CancellationToken stoppingToken) =>
        Task.Delay(delay, stoppingToken);

    /// <summary>
    /// Runs the iteration loop until <paramref name="stoppingToken"/> is cancelled.
    /// </summary>
    /// <param name="stoppingToken">Signaled when the host is stopping.</param>
    /// <returns>A task that completes when the loop ends.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var didSomething = await RunIterationAsync(stoppingToken);
            await LoopIntervalAsync(didSomething ? IntervalRecurring : IntervalDelayed, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }
}
