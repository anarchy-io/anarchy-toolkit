using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Samples.Examples.Telemetry;

/// <summary>
/// Serilog as one more logging provider: the consumer owns the sinks, and the toolkit's own events (call-site
/// prefix, event ids, correlation) reach them. <see cref="CheckoutService"/> is unchanged.
/// </summary>
public static class SerilogConsumerSinkExample
{
    public static async Task<IReadOnlyList<LogEvent>> RunAsync(TextWriter output)
    {
        var sink = new CollectingSink(output);
        var services = new ServiceCollection();
        services.AddSerilogTelemetry(configuration => configuration
            .MinimumLevel.Debug()
            .Enrich.WithStructuredExceptionContext()
            .WriteTo.Sink(sink));
        services.AddSingleton<CheckoutService>();
        await using (var provider = services.BuildServiceProvider())
        {
            await provider.GetRequiredService<CheckoutService>().CheckoutAsync("Ada", 2000);
        }

        return sink.Events;
    }

    /// <summary>The consumer's sink: here it keeps and prints events; in an application it would call a vendor client.</summary>
    private sealed class CollectingSink(TextWriter output) : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
            output.WriteLine($"[sink] {logEvent.Level} {logEvent.RenderMessage()} (event id: {EventId(logEvent)})");
        }

        private static string EventId(LogEvent logEvent) =>
            logEvent.Properties.TryGetValue("EventId", out var value) ? value.ToString() : "-";
    }
}
