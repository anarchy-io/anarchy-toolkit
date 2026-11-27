using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

namespace Anarchy.Toolkit.Samples.Examples.Telemetry;

/// <summary>
/// OTLP export: the default recorder publishes standard .NET metrics on the <see cref="CoreTelemetry.InstrumentationName"/>
/// meter, so OpenTelemetry exports them with <c>AddMeter</c> and an OTLP exporter, and no custom recorder. Point
/// <paramref name="endpoint"/> at an OpenTelemetry Collector or a vendor's OTLP intake and add the headers it asks for.
/// </summary>
public static class OtlpExportExample
{
    public static async Task RunAsync(Uri endpoint, HttpMessageHandler? handler = null)
    {
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(CoreTelemetry.InstrumentationName)
            .AddOtlpExporter((exporter, reader) => {
                exporter.Endpoint = endpoint;
                exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                if (handler is not null) exporter.HttpClientFactory = () => new HttpClient(handler, disposeHandler: false);
                reader.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCoreTelemetry();
        services.AddSingleton<CheckoutService>();
        await using (var provider = services.BuildServiceProvider())
        {
            await provider.GetRequiredService<CheckoutService>().CheckoutAsync("Ada", 42);
        }

        meterProvider.ForceFlush();
    }
}
