using Anarchy.Toolkit.Core.Telemetry;
using StatsdClient;
using System.Globalization;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.StatsD;

/// <summary>Records toolkit metrics using an application-owned DogStatsD client.</summary>
/// <remarks>Durations are samples in seconds, not DogStatsD timers in milliseconds. The client is not disposed here.</remarks>
public sealed class StatsDMetricRecorder(IDogStatsd client, bool useDistributions = false) : IMetricRecorder
{
    private readonly IDogStatsd _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _client.Counter(name, amount, tags: FormatTags(tags));
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _client.Gauge(name, value, tags: FormatTags(tags));
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (useDistributions)
            _client.Distribution(name, value, tags: FormatTags(tags));
        else
            _client.Histogram(name, value, tags: FormatTags(tags));
    }

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags) =>
        RecordHistogram(name, duration.TotalSeconds, tags);

    private static string[] FormatTags(KeyValuePair<string, object?>[] tags) =>
        tags.Where(tag => tag.Value is not null)
            .Select(tag => $"{tag.Key}:{FormatValue(tag.Value!)}")
            .ToArray();

    private static string FormatValue(object value) => value switch {
        bool boolean => boolean ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
