using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Anarchy.Toolkit.Samples.Worker.Infrastructure.Entity.Configuration;

namespace Anarchy.Toolkit.Samples.Worker.Core.Service.Worker;

/// <summary>
/// Greets every 10 seconds; the monitored base records start, stop and failure metrics around each iteration.
/// </summary>
public sealed class GreetingWorker(AppSettings settings, Observer observer)
    : MonitoredWorkerBase(observer, recurringIntervalInSeconds: 10, delayedIntervalInSeconds: 10)
{
    protected override Task<bool> RunIterationAsync(CancellationToken stoppingToken)
    {
        Observer.LogInformation(this.CalledFrom(), "{Greeting} from the worker", settings.Greeting);

        return Task.FromResult(true);
    }
}
