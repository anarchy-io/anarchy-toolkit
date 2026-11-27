using Anarchy.Toolkit.Samples.Examples.Telemetry;
using AwesomeAssertions;
using System.Net;

namespace Anarchy.Toolkit.Samples.Testing.Telemetry;

/// <summary>Runs the telemetry examples and checks what reached each provider, without any external backend.</summary>
public class TelemetryExamplesTest
{
    [Fact]
    public async Task DefaultProvider_WritesLogsAndStandardMetrics()
    {
        var output = new StringWriter();

        await DefaultProviderExample.RunAsync(output);

        output.ToString().Should().Contain("Checking out Ada").And.Contain("execution_count");
    }

    [Fact]
    public async Task Serilog_DeliversTheToolkitEventsToTheConsumerSink()
    {
        var events = await SerilogConsumerSinkExample.RunAsync(new StringWriter());

        events.Should().Contain(logEvent => logEvent.MessageTemplate.Text.Contains("Payment declined"));
    }

    [Fact]
    public async Task ProviderSwap_RunsTheSameInstrumentationOnBothProviders()
    {
        var output = new StringWriter();

        await ProviderSwapExample.RunAsync(output);

        output.ToString().Split(Environment.NewLine).Count(line => line.Contains("Checking out")).Should().Be(2);
    }

    [Fact]
    public async Task CustomRecorder_ReceivesTheToolkitMetrics()
    {
        var names = await CustomMetricRecorderExample.RunAsync(new StringWriter());

        names.Should().Contain(name => name.EndsWith(".execution_count"))
             .And.Contain(name => name.EndsWith(".declined"));
    }

    [Fact]
    public async Task Otlp_ExportsTheStandardMetricsToTheEndpoint()
    {
        var receiver = new Receiver();

        await OtlpExportExample.RunAsync(new Uri("http://localhost:4318/v1/metrics"), receiver);

        receiver.Path.Should().Be("/v1/metrics");
        receiver.Body.Should().NotBeEmpty();
    }

    private sealed class Receiver : HttpMessageHandler
    {
        public string? Path { get; private set; }

        public byte[] Body { get; private set; } = [];

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            Body = request.Content?.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult() ?? [];
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(Send(request, cancellationToken));
    }
}
