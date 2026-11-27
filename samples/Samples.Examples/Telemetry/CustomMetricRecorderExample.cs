using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Samples.Examples.Telemetry;

/// <summary>
/// A custom <see cref="IMetricRecorder"/>: register it and every toolkit package, and the application's own
/// <see cref="AppTelemetry"/> calls, send their metrics to it. Use it to reach a metrics client the standard .NET
/// pipeline does not cover, or decorate the default recorder to also forward each metric.
/// </summary>
public static class CustomMetricRecorderExample
{
    public static async Task<IReadOnlyList<string>> RunAsync(TextWriter output)
    {
        var recorder = new ConsoleMetricRecorder(output);
        var services = new ServiceCollection();
        services.AddSingleton<IMetricRecorder>(recorder);
        services.AddCoreTelemetry();
        services.AddSingleton<CheckoutService>();
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CheckoutService>().CheckoutAsync("Ada", 2000);

        return recorder.Names;
    }

    /// <summary>Prints each metric; a real recorder would call a vendor client here.</summary>
    public sealed class ConsoleMetricRecorder(TextWriter output) : IMetricRecorder
    {
        public List<string> Names { get; } = [];

        public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags) =>
            Write("counter", name, amount, tags);

        public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags) =>
            Write("gauge", name, value, tags);

        public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags) =>
            Write("histogram", name, value, tags);

        public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags) =>
            Write("duration", name, duration.TotalSeconds, tags);

        private void Write(string kind, string name, double value, KeyValuePair<string, object?>[] tags)
        {
            Names.Add(name);
            output.WriteLine($"[{kind}] {name} {value:0.###} {string.Join(',', tags.Select(tag => $"{tag.Key}={tag.Value}"))}");
        }
    }
}
