using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Newtonsoft.Json;
using System.Globalization;

namespace Anarchy.Toolkit.Configuration.Service.Observability;

/// <summary>
/// Local development and debugging metrics sink.
/// This implementation favors readable console output and tolerates malformed metrics/tags by skipping invalid input.
/// It is not intended for production telemetry pipelines.
/// </summary>
public sealed class ConsoleMeasurer(bool tagsAsJson = false) : MeasurerBase
{
    private const string Reset = "\u001b[0m";
    private const string Bold = "\u001b[1m";
    private const string Blue = "\u001b[34m";
    private const string Cyan = "\u001b[36m";
    private const string Green = "\u001b[32m";
    private const string Purple = "\u001B[38;5;0200m";
    private const string Gray = "\u001b[90m";

    public override void IncrementCounter(string name, params string[] tags)
    {
        IncrementCounter(name, 1, tags);
    }

    public override void IncrementCounter(string name, int amount, params string[] tags)
    {
        WriteMetric("counter", name, amount.ToString(CultureInfo.InvariantCulture), tags);
    }

    public override void Gauge(string name, double value, params string[] tags)
    {
        WriteMetric("gauge", name, value.ToString(CultureInfo.InvariantCulture), tags);
    }

    public override void Histogram(string name, double value, params string[] tags)
    {
        WriteMetric("histogram", name, value.ToString(CultureInfo.InvariantCulture), tags);
    }

    public override void Timer(string name, TimeSpan elapsed, params string[] tags)
    {
        WriteMetric("timer", name, elapsed.ToString("c", CultureInfo.InvariantCulture), tags);
    }

    public override void Event(string name, string text, params string[] tags)
    {
        WriteMetric("event", name, text, tags);
    }

    private void WriteMetric(string? type, string? name, string? value, string[]? tags)
    {
        var cleanType = CleanupMetricField(type);
        var cleanName = CleanupMetricField(name);

        if (cleanType is null || cleanName is null)
        {
            return;
        }

        var cleanValue = value?.Trim() ?? string.Empty;
        var cleanTags = SanitizeTags(tags);

        var time = DateTimeOffset.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

        var tagsSuffix = cleanTags.Length > 0 ? $"with {Bold}Tags{Reset}:" : $"with no {Bold}Tags{Reset}";

        Console.WriteLine($"[{time} {Green}{Bold}METRIC{Reset}{Gray}]{Reset} Collected '{Bold}{Blue}{cleanType}{Reset}': {Cyan}{cleanName}{Reset} → {Purple}{cleanValue}{Reset} {tagsSuffix}");

        if (cleanTags.Length <= 0)
        {
            return;
        }

        Console.WriteLine(tagsAsJson ? TagsToJson(cleanTags) : FormatTags(cleanTags));
    }

    private static string? CleanupMetricField(string? input)
    {
        var clean = input?.Trim();
        return string.IsNullOrWhiteSpace(clean) ? null : clean;
    }

    private static string[] SanitizeTags(string[]? tags)
    {
        if (tags is null || tags.Length == 0)
        {
            return [];
        }

        return tags
            .Select(ParseTag)
            .Where(parsed => parsed is not null)
            .Select(parsed => parsed!)
            .ToArray();
    }

    private static string? ParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var parts = tag.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return null;
        }

        return $"{parts[0]}:{parts[1]}";
    }

    private static string TagsToJson(string[] tags)
    {
        return tags.Select(t => {
            var parts = t.Split(':', 2);
            return new Dictionary<string, string> {
                    { parts[0], parts[1] }
                };
        })
            .ToJson(formatting: Formatting.Indented);
    }

    private static string FormatTags(string[] tags)
    {
        return string.Join(Environment.NewLine, tags
            .Select(t => t.Split(':', 2))
            .Select(parts => $"{Gray}  ↳ {Bold}{parts[0]}{Reset}{Gray} → {parts[1]}{Reset}"));
    }
}
