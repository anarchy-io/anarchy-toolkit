using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

public abstract class WorkerBase(
    double recurringIntervalInSeconds = 2,
    double delayedIntervalInSeconds = 20) : BackgroundService
{
    protected readonly TimeSpan IntervalRecurring = TimeSpan.FromSeconds(recurringIntervalInSeconds);
    protected readonly TimeSpan IntervalDelayed = TimeSpan.FromSeconds(delayedIntervalInSeconds);

    protected abstract Task<bool> RunIterationAsync(CancellationToken stoppingToken);

    protected virtual Task LoopIntervalAsync(TimeSpan delay, CancellationToken stoppingToken) =>
        Task.Delay(delay, stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var didSomething = await RunIterationAsync(stoppingToken);
            await LoopIntervalAsync(didSomething ? IntervalRecurring : IntervalDelayed, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }
}
