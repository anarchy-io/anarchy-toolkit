using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Records a duration once, when disposed.</summary>
/// <remarks>Created by <see cref="AppTelemetry.StartTimer(string, KeyValuePair{string, object?}[])"/>.</remarks>
/// <param name="metrics">The recorder the duration goes to.</param>
/// <param name="name">The metric name.</param>
/// <param name="tags">Low-cardinality tags.</param>
public sealed class MetricTimer(IMetricRecorder metrics, string name, KeyValuePair<string, object?>[] tags) : IDisposable
{
    private readonly long _started = Stopwatch.GetTimestamp();
    private int _disposed;

    /// <summary>The time elapsed since the timer started.</summary>
    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_started);

    /// <summary>Stops the timer and records its duration; subsequent calls do nothing.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            metrics.RecordDuration(name, Elapsed, tags);
    }
}
