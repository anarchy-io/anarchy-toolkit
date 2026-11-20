using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Anarchy.Toolkit.Samples.Worker.Infrastructure.Entity.Configuration;

namespace Anarchy.Toolkit.Samples.Worker.Core.Service.Worker;

/// <summary>
/// Greets every 10 seconds; the monitored base records start, stop and failure metrics around each iteration.
/// </summary>
public sealed class GreetingWorker(AppSettings settings, AppTelemetry telemetry)
    : MonitoredWorkerBase(telemetry, recurringIntervalInSeconds: 10, delayedIntervalInSeconds: 10)
{
    protected override Task<bool> RunIterationAsync(CancellationToken stoppingToken)
    {
        Telemetry.Log(Microsoft.Extensions.Logging.LogLevel.Information, this.CalledFrom(), null, "{Greeting} from the worker", settings.Greeting);

        return Task.FromResult(true);
    }
}
