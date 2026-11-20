using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Telemetry;
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
    private static readonly CallSite CallSite = new("OrderService", "Run");

    [Fact]
    public void AddSerilogTelemetry_UsesTheSuppliedSinkWithoutTakingOwnership()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddSerilogTelemetry(logger);
        services.AddSerilogTelemetry(logger);

        using (var provider = services.BuildServiceProvider())
        {
            var telemetry = provider.GetRequiredService<AppTelemetry>();
            using (telemetry.LogContext.PushProperty("RequestId", "abc"))
                telemetry.Log(LogLevel.Information, CallSite, null, "Order {OrderId}", 42);
        }

        sink.Events.Should().ContainSingle("the same logger is registered once");
        sink.Events[0].Properties.Should().ContainKey("RequestId").And.ContainKey("OrderId");
        logger.Information("Still owned by the caller");
        sink.Events.Should().HaveCount(2);
    }

    [Fact]
    public void AddSerilogTelemetry_WithAConfiguration_OwnsAndFlushesTheLogger()
    {
        var sink = new RecordingSink();
        var services = new ServiceCollection();
        services.AddSerilogTelemetry(configuration => configuration.WriteTo.Sink(sink));

        using (var provider = services.BuildServiceProvider())
            provider.GetRequiredService<AppTelemetry>().LogInformation(CallSite, "Started");

        sink.Events.Should().ContainSingle();
        sink.Disposed.Should().BeTrue("the logger built by the container is disposed with it");
    }

    [Fact]
    public void AddSerilogTelemetry_DoesNotReplaceExistingPrimitives()
    {
        using var capture = new TestTelemetry();
        using var logger = new LoggerConfiguration().CreateLogger();
        var services = new ServiceCollection();
        services.AddSingleton(capture.Core);
        services.AddSerilogTelemetry(logger);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICoreTelemetry>().Should().BeSameAs(capture.Core);
    }

    [Fact]
    public void SameInstrumentation_ReachesSerilogAndANonSerilogProvider()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();
        using var capture = new TestTelemetry();
        var services = new ServiceCollection();
        services.AddSerilogTelemetry(logger);
        services.AddSingleton<ILoggerProvider>(capture.LoggerProvider);

        using var provider = services.BuildServiceProvider();
        Instrumentation(provider.GetRequiredService<AppTelemetry>());

        sink.Events.Should().ContainSingle().Which.RenderMessage().Should().Contain("Processing 42");
        capture.Logs.Should().ContainSingle().Which.Message.Should().Contain("Processing 42");
    }

    [Fact]
    public void ModuleSerilogTelemetry_RegistersTheLoggerAndTheDefaults()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        new ModuleSerilogTelemetry(logger).RegisterServices(services, Mock.Of<IConfiguration>(), Mock.Of<IHostEnvironment>());

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().Log(LogLevel.Information, new CallSite("Orders", "Run"), null, "Started");
        sink.Events.Should().ContainSingle();
        provider.GetRequiredService<IMetricRecorder>().Should().BeOfType<MeterMetricRecorder>();
    }

    [Fact]
    public void AddTelemetryLogMetrics_WritesMetricsToTheLogsAndKeepsTheRegisteredRecorder()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var recorder = new InMemoryMetricRecorder();
        var services = new ServiceCollection();
        services.AddSingleton<IMetricRecorder>(recorder);
        services.AddSerilogTelemetry(logger);
        services.AddTelemetryLogMetrics();
        services.AddTelemetryLogMetrics();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().IncrementCounter("orders.processed", 2, new KeyValuePair<string, object?>("success", true));

        var metricEvent = sink.Events.Should().ContainSingle().Which;
        metricEvent.RenderMessage().Should().Contain("orders.processed");
        metricEvent.Properties["SourceContext"].ToString().Should().Contain(LogMetricRecorder.LoggerCategory);
        recorder.CounterTotal("orders.processed").Should().Be(2, "the decorated recorder still receives the metric");
    }

    [Fact]
    public void AddTelemetryLogMetrics_DecoratesTheDefaultRecorder()
    {
        using var capture = new TestTelemetry();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerProvider>(capture.LoggerProvider);
        services.AddTelemetryLogMetrics(LogLevel.Debug);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().RecordDuration("orders.load", TimeSpan.FromSeconds(2));

        provider.GetRequiredService<IMetricRecorder>().Should().BeOfType<LogMetricRecorder>();
        var log = capture.Logs.Should().ContainSingle().Which;
        log.Level.Should().Be(LogLevel.Debug);
        log.EventId.Should().Be(LogMetricRecorder.MetricEventId);
        log.Properties.Should().Contain("MetricType", "duration").And.Contain("MetricValue", 2d);
    }

    [Fact]
    public void WithStructuredExceptionContext_AddsTheExceptionDetails()
    {
        var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().Enrich.WithStructuredExceptionContext().WriteTo.Sink(sink).CreateLogger();

        logger.Error(new InvalidOperationException("failed"), "Failure");
        logger.Information("No failure");

        sink.Events[0].Properties.Should().ContainKey("Exceptions");
        sink.Events[0].Properties["Exceptions"].ToString().Should().Contain("failed");
        sink.Events[1].Properties.Should().NotContainKey("Exceptions");
    }

    [Fact]
    public void AddSerilogConsoleTelemetry_RespectsAConsumerSuppliedProvider()
    {
        using var capture = new TestTelemetry();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerProvider>(capture.LoggerProvider);

        services.AddSerilogConsoleTelemetry();

        services.Where(descriptor => descriptor.ServiceType == typeof(ILoggerProvider)).Should().ContainSingle();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().LogInformation(CallSite, "Started");
        capture.Logs.Should().ContainSingle();
    }

    [Fact]
    public void AddSerilogConsoleTelemetry_AddsTheConsoleWhenNoProviderIsRegistered()
    {
        var services = new ServiceCollection();

        services.AddSerilogConsoleTelemetry();

        services.Where(descriptor => descriptor.ServiceType == typeof(ILoggerProvider)).Should().ContainSingle();
    }

    private static void Instrumentation(AppTelemetry telemetry) =>
        telemetry.LogInformation(CallSite, "Processing {OrderId}", 42);

    private sealed class RecordingSink : ILogEventSink, IDisposable
    {
        public List<LogEvent> Events { get; } = [];
        public bool Disposed { get; private set; }
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
        public void Dispose() => Disposed = true;
    }
}
