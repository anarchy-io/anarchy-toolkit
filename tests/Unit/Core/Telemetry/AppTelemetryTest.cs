using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Testing.Telemetry;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Tests.Unit.Core.Telemetry;

public class AppTelemetryTest
{
    [Fact]
    public void AddCoreTelemetry_RegistersTheDefaultsFromTheApplicationFactories()
    {
        var services = new ServiceCollection();
        services.AddCoreTelemetry();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMetricRecorder>().Should().BeOfType<MeterMetricRecorder>()
            .Which.Meter.Name.Should().Be(CoreTelemetry.InstrumentationName);
        provider.GetRequiredService<ICoreTelemetry>().Should().BeOfType<CoreTelemetry>();
        provider.GetRequiredService<AppTelemetry>().Core.Should().BeSameAs(provider.GetRequiredService<ICoreTelemetry>());
    }

    [Fact]
    public void AddCoreTelemetry_KeepsARecorderRegisteredBefore()
    {
        var recorder = new InMemoryMetricRecorder();
        var services = new ServiceCollection();
        services.AddSingleton<IMetricRecorder>(recorder);
        services.AddCoreTelemetry();
        services.AddCoreTelemetry();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().IncrementCounter("orders.processed");

        recorder.CounterTotal("orders.processed").Should().Be(1);
    }

    [Fact]
    public void AddCoreTelemetry_LetsARecorderRegisteredAfterReplaceTheDefault()
    {
        var recorder = new InMemoryMetricRecorder();
        var services = new ServiceCollection();
        services.AddCoreTelemetry();
        services.Replace(ServiceDescriptor.Singleton<IMetricRecorder>(recorder));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().IncrementCounter("orders.processed");

        recorder.CounterTotal("orders.processed").Should().Be(1);
    }

    [Fact]
    public void AddCoreTelemetry_KeepsCorePrimitivesRegisteredBefore()
    {
        using var capture = new TestTelemetry();
        var services = new ServiceCollection();
        services.AddSingleton(capture.Core);
        services.AddCoreTelemetry();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().LogInformation(new CallSite("Orders", "Run"), "Started");

        capture.Logs.Should().ContainSingle().Which.Message.Should().Be("[Orders → Run] Started");
    }

    [Fact]
    public void AddCoreTelemetry_RoutesLogsToLoggingProvidersRegisteredBeforeAndAfter()
    {
        using var before = new TestTelemetry();
        using var after = new TestTelemetry();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerProvider>(before.LoggerProvider);
        services.AddCoreTelemetry();
        services.AddSingleton<ILoggerProvider>(after.LoggerProvider);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AppTelemetry>().Log(LogLevel.Warning, new CallSite("OrderService", "Run"), null,
            "Processing {OrderId}", 42);

        foreach (var capture in new[] { before, after })
        {
            var log = capture.Logs.Should().ContainSingle().Which;
            log.Category.Should().Be(CoreTelemetry.InstrumentationName);
            log.Message.Should().Be("[OrderService → Run] Processing 42");
            log.Properties.Should().Contain("OrderId", 42);
        }
    }

    [Fact]
    public void Disposal_NeverDisposesExternallySuppliedInstances()
    {
        var recorder = new DisposableRecorder();
        var loggerProvider = new DisposableLoggerProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IMetricRecorder>(recorder);
        services.AddLogging(builder => builder.AddProvider(loggerProvider));
        services.AddCoreTelemetry();

        using (var provider = services.BuildServiceProvider())
            provider.GetRequiredService<AppTelemetry>().IncrementCounter("orders.processed");

        recorder.Disposed.Should().BeFalse("instances registered by the application are owned by it");
        loggerProvider.Disposed.Should().BeFalse();
        recorder.Count.Should().Be(1);
    }

    [Fact]
    public async Task MonitoredActionAsync_RecordsStandardInstrumentsWithCallSiteNames()
    {
        using var meter = new Meter("Anarchy.Toolkit.Tests." + Guid.NewGuid());
        using var listener = Listen(meter, out var measurements);
        var telemetry = new AppTelemetry(new CoreTelemetry(NullLogger.Instance, new MeterMetricRecorder(meter)));

        var result = await telemetry.MonitoredActionAsync(new CallSite("OrderService", "ProcessAsync"), () => Task.FromResult(42),
            TestContext.Current.CancellationToken);

        result.Should().Be(42);
        measurements.Should().Contain(item => item.Name == "order_service.process_async.execution_count" && item.Value == 1);
        measurements.Should().Contain(item => item.Name == "order_service.process_async.execution_time" && item.Value >= 0
                                              && item.Unit == "s");
    }

    [Fact]
    public async Task MonitoredActionAsync_LogsAndTagsFailuresAndCancellations()
    {
        using var capture = new TestTelemetry();
        using var cancellation = new CancellationTokenSource();
        var callSite = new CallSite("OrderService", "ProcessAsync");

        var failing = () => capture.Telemetry.MonitoredActionAsync(callSite, () => Task.FromException<int>(new InvalidOperationException()));
        await failing.Should().ThrowAsync<InvalidOperationException>();
        await cancellation.CancelAsync();
        var cancelled = () => capture.Telemetry.MonitoredActionAsync(callSite,
            () => Task.FromCanceled<int>(cancellation.Token), cancellation.Token);
        await cancelled.Should().ThrowAsync<OperationCanceledException>();

        capture.Logs.Select(log => log.Level).Should().Equal(LogLevel.Error, LogLevel.Information);
        var counts = capture.Metrics.Named("order_service.process_async.execution_count");
        counts[0].HasTag("success", false).Should().BeTrue();
        counts[1].HasTag("cancelled", true).Should().BeTrue();
    }

    [Fact]
    public void StartTimer_RecordsOnlyOnce()
    {
        using var capture = new TestTelemetry();
        var timer = capture.Telemetry.StartTimer(new CallSite("Orders", "Load"), new KeyValuePair<string, object?>("region", "eu"));

        timer.Dispose();
        timer.Dispose();

        capture.Metrics.Metrics.Should().ContainSingle().Which.Should().Match<RecordedMetric>(metric =>
            metric.Kind == MetricKind.Duration && metric.Name == "orders.load.execution_time" && metric.HasTag("region", "eu"));
    }

    [Fact]
    public void GaugeAndHistogram_UseStandardInstruments()
    {
        using var meter = new Meter("Anarchy.Toolkit.Tests." + Guid.NewGuid());
        using var listener = Listen(meter, out var measurements);
        var telemetry = new AppTelemetry(new CoreTelemetry(NullLogger.Instance, new MeterMetricRecorder(meter)));

        telemetry.RecordGauge("queue.depth", 4);
        telemetry.RecordHistogram(new CallSite("Messages", "Read"), "size", 8);

        measurements.Should().Contain(item => item.Name == "queue.depth" && item.Value == 4)
            .And.Contain(item => item.Name == "messages.read.size" && item.Value == 8);
    }

    [Fact]
    public void MeterMetricRecorder_CachesAnInstrumentByNameAndRejectsDifferentKinds()
    {
        using var meter = new Meter("Anarchy.Toolkit.Tests." + Guid.NewGuid());
        var published = new List<Instrument>();
        using var listener = new MeterListener {
            InstrumentPublished = (instrument, current) => {
                if (instrument.Meter == meter)
                {
                    published.Add(instrument);
                    current.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.Start();
        var recorder = new MeterMetricRecorder(meter);

        recorder.IncrementCounter("orders.processed");
        recorder.IncrementCounter("orders.processed");

        var conflict = () => recorder.RecordHistogram("orders.processed", 1);
        conflict.Should().Throw<InvalidOperationException>()
            .WithMessage("Metric 'orders.processed' is already registered as a counter; a metric name must use one instrument kind.");
        published.Where(instrument => instrument.Name == "orders.processed").Should().ContainSingle();
    }

    [Fact]
    public void Log_WithAnEventId_WritesAStableStructuredEvent()
    {
        using var capture = new TestTelemetry();
        var orderShipped = new EventId(1001, "OrderShipped");

        capture.Telemetry.Log(LogLevel.Information, new CallSite("Orders", "Ship"), orderShipped, null,
            "Order {OrderId} shipped", 42);

        var log = capture.Logs.Should().ContainSingle().Which;
        log.EventId.Should().Be(orderShipped);
        log.EventId.Name.Should().Be("OrderShipped");
        log.Properties.Should().Contain("OrderId", 42);
    }

    [Fact]
    public void LogContext_PushesPropertiesOntoTheLoggingScope()
    {
        using var capture = new TestTelemetry();

        using (capture.Telemetry.LogContext.PushProperty("OrderId", 42))
            capture.Telemetry.LogInformation(new CallSite("Orders", "Run"), "Inside");
        capture.Telemetry.LogInformation(new CallSite("Orders", "Run"), "Outside");

        capture.Logs[0].Scopes.Should().Contain("OrderId", 42);
        capture.Logs[1].Scopes.Should().NotContainKey("OrderId");
    }

    [Fact]
    public async Task LogContext_FlowsAcrossAwait()
    {
        using var capture = new TestTelemetry();

        using (capture.Telemetry.LogContext.PushProperty("OrderId", 42))
        {
            await Task.Yield();
            await Task.Run(() => capture.Telemetry.LogInformation(new CallSite("Orders", "Run"), "Inside"),
                TestContext.Current.CancellationToken);
        }

        capture.Logs.Should().ContainSingle().Which.Scopes.Should().Contain("OrderId", 42);
    }

    [Fact]
    public void Create_BuildsTelemetryWithoutDependencyInjection()
    {
        using var capture = new TestTelemetry();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(capture.LoggerProvider));

        var telemetry = AppTelemetry.Create(loggerFactory);
        telemetry.LogInformation(new CallSite("Tool", "Run"), "Started");
        telemetry.IncrementCounter("tool.runs");

        capture.Logs.Should().ContainSingle().Which.Category.Should().Be(CoreTelemetry.InstrumentationName);
        telemetry.Metrics.Should().BeSameAs(NullMetricRecorder.Instance);
    }

    private static MeterListener Listen(Meter meter, out List<(string Name, double Value, string? Unit)> measurements)
    {
        var captured = new List<(string Name, double Value, string? Unit)>();
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) => {
            if (instrument.Meter == meter) current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => captured.Add((instrument.Name, value, instrument.Unit)));
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => captured.Add((instrument.Name, value, instrument.Unit)));
        listener.Start();
        measurements = captured;
        return listener;
    }

    private sealed class DisposableRecorder : IMetricRecorder, IDisposable
    {
        public bool Disposed { get; private set; }
        public int Count { get; private set; }
        public void IncrementCounter(string name, long amount = 1, params KeyValuePair<string, object?>[] tags) => Count++;
        public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags) { }
        public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags) { }
        public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags) { }
        public void Dispose() => Disposed = true;
    }

    private sealed class DisposableLoggerProvider : ILoggerProvider
    {
        public bool Disposed { get; private set; }
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public void Dispose() => Disposed = true;
    }
}
