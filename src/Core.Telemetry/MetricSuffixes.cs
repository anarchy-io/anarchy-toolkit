namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>
/// The standard metric name suffixes. Combined with a <see cref="CallSite"/> they give names such as
/// <c>order_service.process_async.execution_time</c>.
/// </summary>
public static class MetricSuffixes
{
    /// <summary>Duration of an operation, in seconds: <c>execution_time</c>.</summary>
    public const string ExecutionTime = "execution_time";

    /// <summary>Number of executions of an operation: <c>execution_count</c>.</summary>
    public const string ExecutionCount = "execution_count";

    /// <summary>Failures that ended an operation: <c>failed</c>.</summary>
    public const string Failed = "failed";

    /// <summary>Failed attempts that were retried: <c>failed.action</c>.</summary>
    public const string FailedAction = "failed.action";

    /// <summary>Liveness pings: <c>health.ping</c>.</summary>
    public const string HealthPing = "health.ping";

    /// <summary>Process heartbeats: <c>heartbeat</c>.</summary>
    public const string Heartbeat = "heartbeat";

    /// <summary>Items no handler was found for: <c>no_handlers</c>.</summary>
    public const string NoHandlers = "no_handlers";
}
