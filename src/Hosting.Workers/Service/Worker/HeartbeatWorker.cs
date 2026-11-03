using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

public sealed class HeartbeatWorker(Observer observer, double recurringIntervalInSeconds = 30)
    : WorkerBase(recurringIntervalInSeconds)
{
    protected override Task<bool> RunIterationAsync(CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();

        observer.LogInformation(caller, "My heart is still beating...");
        observer.IncrementCounter(caller, Observer.MetricSuffixHeartbeat, 1, Tags.Success);

        return Task.FromResult(true);
    }
}
