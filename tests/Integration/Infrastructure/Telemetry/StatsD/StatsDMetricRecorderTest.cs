using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Telemetry.StatsD.Extensions.Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using StatsdClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Anarchy.Toolkit.Tests.Integration.Infrastructure.Telemetry.StatsD;

public class StatsDMetricRecorderTest
{
    [Fact]
    public void AddStatsDTelemetry_WhenConfigurationIsInvalid_ThrowsOnResolution()
    {
        var services = new ServiceCollection();
        services.AddStatsDTelemetry(new StatsdConfig { StatsdServerName = "127.0.0.1", StatsdPort = 70000 });
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<AppTelemetry>();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Could not configure the DogStatsD client.")
            .WithInnerException<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(false, "h")]
    [InlineData(true, "d")]
    public async Task AppTelemetry_EmitsDogStatsDPackets(bool distributions, string sampleType)
    {
        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)receiver.Client.LocalEndPoint!).Port;
        var services = new ServiceCollection();
        services.AddStatsDTelemetry(new StatsdConfig {
            StatsdServerName = "127.0.0.1",
            StatsdPort = port,
            Prefix = "test",
            ClientSideAggregation = null,
            SynchronousMode = true
        }, distributions);
        using var provider = services.BuildServiceProvider();
        var metrics = provider.GetRequiredService<AppTelemetry>().Metrics;
        KeyValuePair<string, object?>[] tags = [new("success", true)];
        metrics.IncrementCounter("requests", 2, tags);
        metrics.RecordGauge("depth", 3, tags);
        metrics.RecordHistogram("size", 4, tags);
        metrics.RecordDuration("duration", TimeSpan.FromSeconds(1.5), tags);
        provider.GetRequiredService<IDogStatsd>().Flush();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var packets = new StringBuilder();
        string[] expected = ["test.requests:2|c|#success:true", "test.depth:3|g|#success:true",
            $"test.size:4|{sampleType}|#success:true", $"test.duration:1.5|{sampleType}|#success:true"];
        while (expected.Any(line => !packets.ToString().Contains(line, StringComparison.Ordinal)))
        {
            var received = await receiver.ReceiveAsync(timeout.Token);
            packets.AppendLine(Encoding.UTF8.GetString(received.Buffer));
        }

        foreach (var line in expected)
            packets.ToString().Should().Contain(line);
    }
}
