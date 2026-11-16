using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

/// <summary>
/// Worker that periodically logs a heartbeat message and increments the <c>heartbeat</c> counter, proving the process
/// is alive.
/// </summary>
/// <example>
/// <code language="csharp">
/// services.AddHostedService&lt;HeartbeatWorker&gt;();
/// </code>
/// </example>
/// <param name="observer">The observer used for logs and metrics.</param>
/// <param name="recurringIntervalInSeconds">Interval between heartbeats. Defaults to 30 seconds.</param>
public sealed class HeartbeatWorker(Observer observer, double recurringIntervalInSeconds = 30)
    : WorkerBase(recurringIntervalInSeconds)
{
    /// <inheritdoc />
    protected override Task<bool> RunIterationAsync(CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();

        observer.LogInformation(caller, "My heart is still beating...");
        observer.IncrementCounter(caller, Observer.MetricSuffixHeartbeat, 1, Tags.Success);

        return Task.FromResult(true);
    }
}
