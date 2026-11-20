using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Testing.Telemetry;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.DependencyInjection.Module.Configuration.Service;

public class ModuleTelemetryTest
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder().Build();
    private static readonly IHostEnvironment Environment = Mock.Of<IHostEnvironment>();

    [Fact]
    public void RegisterServices_CreatesTheDefaultsWithoutAdditionalSetup()
    {
        var services = new ServiceCollection();
        services.UseModule(new ModuleTelemetry(), Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().Should().NotBeNull();
        provider.GetRequiredService<IMetricRecorder>().Should().BeOfType<MeterMetricRecorder>();
        provider.GetRequiredService<IApplicationContext>().Should().NotBeNull();
        provider.GetRequiredService<CorrelationIdOptions>().Should().NotBeNull();
        provider.GetServices<ILoggerProvider>().Should().BeEmpty("no console or other sink is imposed");
    }

    [Fact]
    public void RegisterServices_KeepsTheHostLoggingProvidersAndPrimitives()
    {
        using var capture = new TestTelemetry();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerProvider>(capture.LoggerProvider);
        services.AddSingleton<IMetricRecorder>(capture.Metrics);

        services.UseModule(new ModuleTelemetry(), Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        var telemetry = provider.GetRequiredService<AppTelemetry>();
        telemetry.LogInformation(new CallSite("Orders", "Handle"), "Processing {OrderId}", 42);
        telemetry.IncrementCounter("orders.processed");
        capture.Logs.Should().ContainSingle().Which.Message.Should().Contain("Processing 42");
        capture.Metrics.CounterTotal("orders.processed").Should().Be(1);
    }

    [Fact]
    public void RegisterServices_AppliesTheApplicationTelemetryModule()
    {
        using var capture = new TestTelemetry();
        var services = new ServiceCollection();

        services.UseModule(new ModuleTelemetry(new CaptureTelemetryModule(capture)), Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().IncrementCounter(new CallSite("Orders", "Handle"), MetricSuffixes.ExecutionCount);
        capture.Metrics.CounterTotal("orders.handle.execution_count").Should().Be(1);
    }

    [Fact]
    public void RegisterServices_UsesTheGivenCorrelationOptions()
    {
        var services = new ServiceCollection();

        services.UseModule(new ModuleTelemetry(correlationIdOptions: _ => new CorrelationIdOptions { CorrelationIdHeaderName = "X-Acme" }),
            Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<CorrelationIdOptions>().CorrelationIdHeaderName.Should().Be("X-Acme");
    }

    private sealed class CaptureTelemetryModule(TestTelemetry capture) : ModuleTelemetryBase
    {
        protected override void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration,
                                                      IHostEnvironment environment) =>
            services.AddTestTelemetry(capture);
    }
}
