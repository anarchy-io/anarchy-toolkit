using Anarchy.Toolkit.Core.Telemetry;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Tests.Unit.Core.Telemetry;

public class AppTelemetryTest
{
    [Fact]
    public void AddAppTelemetry_DoesNotReplaceAnExistingFacade()
    {
        var existing = Mock.Of<IAppTelemetry>();
        var services = new ServiceCollection();
        services.AddSingleton(existing);
        services.AddAppTelemetry();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Should().BeSameAs(existing);
    }

    [Fact]
    public void AddAppTelemetry_UsesRegisteredLoggingAndMeterFactories()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddAppTelemetry();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Should().BeOfType<AppTelemetry>();
    }

    [Fact]
    public void AddAppTelemetry_RoutesLogsToTheRegisteredProvider()
    {
        var logger = new RecordingLogger();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ClearProviders().AddProvider(new RecordingLoggerProvider(logger)));
        services.AddMetrics();
        services.AddAppTelemetry();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Log(LogLevel.Information, new CallSite("OrderService", "Run"), null,
            "Processing {OrderId}", 42);

        logger.Messages.Should().ContainSingle().Which.Should().Contain("[OrderService → Run]").And.Contain("42");
    }

    [Fact]
    public async Task MonitoredActionAsync_RecordsStandardInstrumentsWithCallSiteNames()
    {
        using var meter = new Meter("Anarchy.Toolkit.Tests." + Guid.NewGuid());
        using var listener = new MeterListener();
        var measurements = new List<(string Name, object Value)>();
        listener.InstrumentPublished = (instrument, current) => {
            if (instrument.Meter == meter) current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => measurements.Add((instrument.Name, value)));
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => measurements.Add((instrument.Name, value)));
        listener.Start();

        var telemetry = new AppTelemetry(NullLogger.Instance, meter);
        var result = await telemetry.MonitoredActionAsync(new CallSite("OrderService", "ProcessAsync"), () => Task.FromResult(42),
            TestContext.Current.CancellationToken);

        result.Should().Be(42);
        measurements.Should().Contain(item => item.Name == "order_service.process_async.execution_count" && (long)item.Value == 1);
        measurements.Should().Contain(item => item.Name == "order_service.process_async.execution_time" && (double)item.Value >= 0);
    }

    [Fact]
    public void StartTimer_RecordsOnlyOnce()
    {
        var telemetry = new Mock<IAppTelemetry>();
        var timer = new MetricTimer(telemetry.Object, "operation.duration", []);

        timer.Dispose();
        timer.Dispose();

        telemetry.Verify(t => t.RecordDuration("operation.duration", It.IsAny<TimeSpan>(), It.IsAny<KeyValuePair<string, object?>[]>()), Times.Once);
    }

    [Fact]
    public void GaugeAndHistogram_UseStandardInstruments()
    {
        using var meter = new Meter("Anarchy.Toolkit.Tests." + Guid.NewGuid());
        using var listener = new MeterListener();
        var measurements = new List<(string Name, double Value)>();
        listener.InstrumentPublished = (instrument, current) => {
            if (instrument.Meter == meter) current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => measurements.Add((instrument.Name, value)));
        listener.Start();
        IAppTelemetry telemetry = new AppTelemetry(NullLogger.Instance, meter);

        telemetry.Gauge("queue.depth", 4);
        telemetry.Histogram("message.size", 8);

        measurements.Should().Contain(("queue.depth", 4)).And.Contain(("message.size", 8));
    }

    [Fact]
    public void RecordEvent_WritesTheTitleAndTextThroughTheRegisteredLogger()
    {
        var logger = new RecordingLogger();
        using var meter = new Meter("Anarchy.Toolkit.Tests." + Guid.NewGuid());
        IAppTelemetry telemetry = new AppTelemetry(logger, meter);

        telemetry.RecordEvent("started", "operation started");

        logger.Messages.Should().ContainSingle().Which.Should().Contain("started").And.Contain("operation started");
    }

    [Fact]
    public async Task LogMetrics_UsesTheConfiguredLoggerAndIsolatesServiceProviders()
    {
        var firstLogger = new RecordingLogger();
        var secondLogger = new RecordingLogger();
        static ServiceProvider CreateProvider(RecordingLogger logger)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.ClearProviders().AddProvider(new RecordingLoggerProvider(logger)));
            services.AddMetrics();
            services.AddAppTelemetry();
            services.AddTelemetryLogMetrics();
            services.AddTelemetryLogMetrics();
            return services.BuildServiceProvider();
        }

        using var first = CreateProvider(firstLogger);
        using var second = CreateProvider(secondLogger);
        var firstListener = first.GetServices<IHostedService>().Should().ContainSingle().Which;
        var secondListener = second.GetServices<IHostedService>().Should().ContainSingle().Which;
        await firstListener.StartAsync(TestContext.Current.CancellationToken);
        await secondListener.StartAsync(TestContext.Current.CancellationToken);

        first.GetRequiredService<IAppTelemetry>().IncrementCounter("first.count");
        second.GetRequiredService<IAppTelemetry>().IncrementCounter("second.count");

        firstLogger.Messages.Should().ContainSingle().Which.Should().Contain("first.count");
        secondLogger.Messages.Should().ContainSingle().Which.Should().Contain("second.count");
        await firstListener.StopAsync(TestContext.Current.CancellationToken);
        await secondListener.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class RecordingLoggerProvider(RecordingLogger logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => logger;
        public void Dispose() { }
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
