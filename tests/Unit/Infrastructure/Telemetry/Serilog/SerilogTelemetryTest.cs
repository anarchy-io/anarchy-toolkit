using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Telemetry.Serilog;

public class SerilogTelemetryTest
{
    [Fact]
    public void AddSerilogTelemetry_UsesTheSuppliedSinkWithoutTakingOwnership()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        services.AddSerilogTelemetry(logger);

        using (var provider = services.BuildServiceProvider())
        {
            var telemetry = provider.GetRequiredService<IAppTelemetry>();
            using (telemetry.BeginScope([new("RequestId", "abc")]))
                telemetry.Log(LogLevel.Information, new CallSite("OrderService", "Run"), null, "Order {OrderId}", 42);
        }

        sink.Events.Should().ContainSingle();
        sink.Events[0].Properties.Should().ContainKey("RequestId");
        logger.Information("Still owned by the caller");
        sink.Events.Should().HaveCount(2);
    }

    [Fact]
    public void AddSerilogTelemetry_DoesNotReplaceAnExistingFacade()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var existing = Mock.Of<IAppTelemetry>();
        var services = new ServiceCollection();
        services.AddSingleton(existing);
        services.AddSerilogTelemetry(logger);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Should().BeSameAs(existing);
    }

    [Fact]
    public void ModuleSerilogTelemetry_UsesTheSameProviderRegistration()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddMetrics();
        new ModuleSerilogTelemetry(logger).RegisterServices(services, Mock.Of<IConfiguration>(), Mock.Of<IHostEnvironment>());

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Log(LogLevel.Information, new CallSite("Orders", "Run"), null, "Started");
        sink.Events.Should().ContainSingle();
    }

    [Fact]
    public async Task LogMetrics_UsesTheConsumerSerilogSink()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        services.AddTelemetryLogMetrics();
        using var provider = services.BuildServiceProvider();
        var listener = provider.GetServices<IHostedService>().Should().ContainSingle().Which;
        await listener.StartAsync(TestContext.Current.CancellationToken);

        provider.GetRequiredService<IAppTelemetry>().IncrementCounter("orders.processed");

        sink.Events.Should().ContainSingle().Which.RenderMessage().Should().Contain("orders.processed");
        await listener.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class RecordingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
