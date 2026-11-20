using Anarchy.Toolkit.Core.Telemetry;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using System.Net;
using System.Text;

namespace Anarchy.Toolkit.Tests.Unit.Core.Telemetry;

public class OtlpTelemetryTest
{
    [Fact]
    public void StandardMetrics_ExportToAnOtlpHttpReceiverWithoutACustomRecorder()
    {
        using var receiver = new Receiver();
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(CoreTelemetry.InstrumentationName)
            .AddOtlpExporter(options => {
                options.Endpoint = new Uri("http://localhost:4318/v1/metrics");
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
                options.HttpClientFactory = () => new HttpClient(receiver, disposeHandler: false);
            })
            .Build();
        var services = new ServiceCollection();
        services.AddCoreTelemetry();
        using var provider = services.BuildServiceProvider();
        var name = "orders.processed." + Guid.NewGuid().ToString("N");

        provider.GetRequiredService<AppTelemetry>().IncrementCounter(name);
        provider.GetRequiredService<AppTelemetry>().RecordDuration(name + ".duration", TimeSpan.FromMilliseconds(5));
        meterProvider.ForceFlush().Should().BeTrue();

        receiver.Path.Should().Be("/v1/metrics");
        receiver.ContentType.Should().Be("application/x-protobuf");
        receiver.Body.Should().NotBeNull();
        Encoding.UTF8.GetString(receiver.Body!).Should().Contain(name).And.Contain(name + ".duration");
    }

    private sealed class Receiver : HttpMessageHandler
    {
        public string? Path { get; private set; }
        public string? ContentType { get; private set; }
        public byte[]? Body { get; private set; }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content?.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(Send(request, cancellationToken));
    }
}
