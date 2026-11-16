namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

public partial class Observer
{
    /// <summary>Metric name suffix for execution durations (<c>execution_time</c>).</summary>
    public const string MetricSuffixExecTime = "execution_time";

    /// <summary>Metric name suffix for execution counts (<c>execution_count</c>).</summary>
    public const string MetricSuffixExecCount = "execution_count";

    /// <summary>Metric name suffix for failures (<c>failed</c>).</summary>
    public const string MetricSuffixFailed = "failed";

    /// <summary>Metric name suffix for failed actions (<c>failed.action</c>).</summary>
    public const string MetricSuffixFailedAction = "failed.action";

    /// <summary>Metric name suffix for health pings (<c>health.ping</c>).</summary>
    public const string MetricSuffixHealthPing = "health.ping";

    /// <summary>Metric name suffix for heartbeats (<c>heartbeat</c>).</summary>
    public const string MetricSuffixHeartbeat = "heartbeat";

    /// <summary>Metric name suffix used when no handler was found for an item (<c>no_handlers</c>).</summary>
    public const string MetricSuffixNoHandlers = "no_handlers";

    /// <summary>Metric name suffix for duplicated updates (<c>duplicated_update</c>).</summary>
    public const string MetricSuffixDuplicatedUpdate = "duplicated_update";

    /// <summary>Metric name suffix for generic durations (<c>duration</c>).</summary>
    public const string MetricSuffixDuration = "duration";
}
