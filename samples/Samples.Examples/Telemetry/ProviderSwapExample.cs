using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Samples.Examples.Telemetry;

/// <summary>
/// Swapping the logging provider changes only the composition root: the same <see cref="CheckoutService"/> runs on
/// Microsoft logging, then on Serilog, without touching the instrumentation.
/// </summary>
public static class ProviderSwapExample
{
    public static async Task RunAsync(TextWriter output)
    {
        output.WriteLine("-- Microsoft logging provider");
        await CheckoutAsync(services => services.AddLogging(logging =>
            logging.AddProvider(new TextWriterLoggerProvider(output))));

        output.WriteLine("-- Serilog provider");
        await CheckoutAsync(services => services.AddSerilogTelemetry(configuration => configuration
            .WriteTo.Sink(new LineSink(output))));
    }

    private static async Task CheckoutAsync(Action<IServiceCollection> configureProvider)
    {
        var services = new ServiceCollection();
        configureProvider(services);
        services.AddCoreTelemetry();
        services.AddSingleton<CheckoutService>();
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CheckoutService>().CheckoutAsync("Ada", 42);
    }

    private sealed class LineSink(TextWriter output) : Serilog.Core.ILogEventSink
    {
        public void Emit(Serilog.Events.LogEvent logEvent) => output.WriteLine($"[{logEvent.Level}] {logEvent.RenderMessage()}");
    }
}
