using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Testing.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Testing.Telemetry;

/// <summary>
/// Telemetry for tests: an <see cref="AppTelemetry"/> whose logs and metrics are captured in memory, so tests can pass
/// it to the code under test and assert on what was emitted.
/// </summary>
/// <remarks>
/// Use <see cref="Telemetry"/> directly, or register the same capture in a service collection with
/// <see cref="TestTelemetryServiceCollectionExtensions.AddTestTelemetry"/>. Dispose it to release its logger factory.
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var telemetry = new TestTelemetry();
/// await new OrderService(telemetry.Telemetry).LoadAsync(42, token);
/// telemetry.Logs.Should().Contain(log => log.Message.Contains("Loaded order 42"));
/// telemetry.Metrics.CounterTotal("order_service.load_async.execution_count").Should().Be(1);
/// </code>
/// </example>
public sealed class TestTelemetry : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Creates the in-memory capture.</summary>
    public TestTelemetry()
    {
        LoggerProvider = new InMemoryLoggerProvider();
        Metrics = new InMemoryMetricRecorder();
        _loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(LoggerProvider));
        Core = new CoreTelemetry(_loggerFactory.CreateLogger(CoreTelemetry.InstrumentationName), Metrics);
        Telemetry = new AppTelemetry(Core);
    }

    /// <summary>The telemetry to pass to the code under test.</summary>
    public AppTelemetry Telemetry { get; }

    /// <summary>The primitives behind <see cref="Telemetry"/>.</summary>
    public ICoreTelemetry Core { get; }

    /// <summary>The logging provider capturing the logs.</summary>
    public InMemoryLoggerProvider LoggerProvider { get; }

    /// <summary>The recorder capturing the metrics.</summary>
    public InMemoryMetricRecorder Metrics { get; }

    /// <summary>Every log event captured so far.</summary>
    public IReadOnlyList<RecordedLog> Logs => LoggerProvider.Logs;

    /// <summary>Discards the captured logs and metrics.</summary>
    public void Clear()
    {
        LoggerProvider.Clear();
        Metrics.Clear();
    }

    /// <inheritdoc />
    public void Dispose() => _loggerFactory.Dispose();
}
