using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Records a duration once when disposed.</summary>
public sealed class MetricTimer(IAppTelemetry telemetry, string name, KeyValuePair<string, object?>[] tags) : IDisposable
{
    private readonly long _started = Stopwatch.GetTimestamp();
    private int _disposed;

    /// <summary>Stops the timer and records its duration; subsequent calls do nothing.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            telemetry.RecordDuration(name, Stopwatch.GetElapsedTime(_started), tags);
    }
}
