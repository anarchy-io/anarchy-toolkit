using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Samples.Examples.Telemetry;

/// <summary>
/// The default provider: <c>AddCoreTelemetry()</c> builds <see cref="ICoreTelemetry"/> and <see cref="AppTelemetry"/>
/// from the application's Microsoft logging and standard .NET metrics. Logs go to whatever logging providers the
/// application registers; metrics are <see cref="System.Diagnostics.Metrics"/> instruments, read here with a
/// <see cref="MeterListener"/> (OpenTelemetry does the same, see <see cref="OtlpExportExample"/>).
/// </summary>
public static class DefaultProviderExample
{
    public static async Task RunAsync(TextWriter output)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(new TextWriterLoggerProvider(output)));
        services.AddCoreTelemetry();
        services.AddSingleton<CheckoutService>();
        await using var provider = services.BuildServiceProvider();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name == CoreTelemetry.InstrumentationName) meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            output.WriteLine($"[metric] {instrument.Name} += {value}"));
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
            output.WriteLine($"[metric] {instrument.Name} = {value:0.###}{instrument.Unit}"));
        listener.Start();

        await provider.GetRequiredService<CheckoutService>().CheckoutAsync("Ada", 42);
    }
}
