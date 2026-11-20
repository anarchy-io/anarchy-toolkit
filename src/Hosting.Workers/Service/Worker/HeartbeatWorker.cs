using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Logging;

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
/// <param name="telemetry">Application telemetry used for logs and metrics.</param>
/// <param name="recurringIntervalInSeconds">Interval between heartbeats. Defaults to 30 seconds.</param>
public sealed class HeartbeatWorker(AppTelemetry telemetry, double recurringIntervalInSeconds = 30)
    : WorkerBase(recurringIntervalInSeconds)
{
    /// <inheritdoc />
    protected override Task<bool> RunIterationAsync(CancellationToken stoppingToken)
    {
        var caller = this.CalledFrom();

        telemetry.Log(LogLevel.Information, caller, null, "My heart is still beating...");
        telemetry.IncrementCounter(caller, MetricSuffixes.Heartbeat, 1, [new("success", true)]);

        return Task.FromResult(true);
    }
}
