using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Telemetry.StatsD;
using Anarchy.Toolkit.Infrastructure.Telemetry.StatsD.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Telemetry.StatsD.Extensions.Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using StatsdClient;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Telemetry.StatsD.Extensions.Microsoft.Extensions.DependencyInjection;

public class StatsDTelemetryExtensionsTest
{
    [Fact]
    public void AddStatsDTelemetry_AfterCoreTelemetry_SelectsRecorderForAppTelemetry()
    {
        var client = new Mock<IDogStatsd>();
        var services = new ServiceCollection();
        services.AddCoreTelemetry();
        services.AddStatsDTelemetry(new StatsdConfig());
        services.AddSingleton(client.Object);

        using (var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }))
        {
            var telemetry = provider.GetRequiredService<AppTelemetry>();
            telemetry.Metrics.Should().BeOfType<StatsDMetricRecorder>();
            telemetry.Metrics.IncrementCounter("requests", 2);
        }

        client.Verify(c => c.Counter("requests", 2, 1, It.IsAny<string[]>(), null, null), Times.Once);
        client.Verify(c => c.Dispose(), Times.Never);
    }

    [Fact]
    public void AddStatsDTelemetry_WhenRecorderRegisteredLater_LaterRecorderWins()
    {
        var services = new ServiceCollection();
        services.AddStatsDTelemetry(new StatsdConfig());
        services.AddSingleton<IMetricRecorder>(NullMetricRecorder.Instance);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().Metrics.Should().BeSameAs(NullMetricRecorder.Instance);
    }

    [Fact]
    public void ModuleStatsDTelemetry_UsesCallerConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Metrics:Host"] = "example.invalid"
        }).Build();
        var services = new ServiceCollection();
        string? host = null;
        new ModuleStatsDTelemetry(config => {
            host = config["Metrics:Host"];
            return new StatsdConfig { StatsdServerName = host };
        }).RegisterServices(services, configuration, Mock.Of<IHostEnvironment>());
        services.AddSingleton(Mock.Of<IDogStatsd>());

        host.Should().Be("example.invalid");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        provider.GetRequiredService<AppTelemetry>().Should().NotBeNull();
    }
}
