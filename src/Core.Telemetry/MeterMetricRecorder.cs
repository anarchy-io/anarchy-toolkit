using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Records metrics as standard <see cref="System.Diagnostics.Metrics"/> instruments.</summary>
/// <remarks>
/// <para>Instruments are created once per name and cached. Durations are histograms in seconds (unit <c>s</c>).</para>
/// <para>The meter is never disposed here: a meter created by an <see cref="IMeterFactory"/> belongs to the factory,
/// and a meter passed in directly belongs to the caller.</para>
/// </remarks>
public sealed class MeterMetricRecorder : IMetricRecorder
{
    private readonly Meter _meter;
    private readonly ConcurrentDictionary<string, Lazy<CachedInstrument>> _instruments = new(StringComparer.Ordinal);

    /// <summary>Records through the meter named <see cref="CoreTelemetry.InstrumentationName"/>, created by the factory.</summary>
    /// <param name="meterFactory">The application's meter factory, which owns the meter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="meterFactory"/> is <see langword="null"/>.</exception>
    public MeterMetricRecorder(IMeterFactory meterFactory)
        : this((meterFactory ?? throw new ArgumentNullException(nameof(meterFactory)))
            .Create(new MeterOptions(CoreTelemetry.InstrumentationName)))
    {
    }

    /// <summary>Records through a meter owned by the caller.</summary>
    /// <param name="meter">The meter; it is not disposed by this recorder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public MeterMetricRecorder(Meter meter)
    {
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));
    }

    /// <summary>The meter the instruments belong to.</summary>
    public Meter Meter => _meter;

    /// <inheritdoc />
    public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags) =>
        Get(name, "counter", metric => _meter.CreateCounter<long>(metric)).Add(amount, tags);

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags) =>
        Get(name, "gauge", metric => _meter.CreateGauge<double>(metric)).Record(value, tags);

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags) =>
        Get(name, "histogram", metric => _meter.CreateHistogram<double>(metric)).Record(value, tags);

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags) =>
        Get(name, "duration", metric => _meter.CreateHistogram<double>(metric, "s")).Record(duration.TotalSeconds, tags);

    private TInstrument Get<TInstrument>(string name, string kind, Func<string, TInstrument> create)
        where TInstrument : Instrument
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var cached = _instruments.GetOrAdd(name,
            metric => new Lazy<CachedInstrument>(
                () => new CachedInstrument(kind, create(metric)), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        return cached.Kind == kind && cached.Instrument is TInstrument typed
            ? typed
            : throw new InvalidOperationException(
                $"Metric '{name}' is already registered as a {cached.Kind}; a metric name must use one instrument kind.");
    }

    private sealed record CachedInstrument(string Kind, Instrument Instrument);
}
