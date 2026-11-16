using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper;

/// <summary>
/// An <see cref="Observer"/> that records log events and metrics in memory so tests can assert on them.
/// </summary>
/// <remarks>
/// Logs are captured at <see cref="LogEventLevel.Verbose"/> level, enriched from the log context, into <see cref="SinkLogs"/>;
/// metrics go to <see cref="InMemoryMeasurer"/>. Create instances via <see cref="Create"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var observer = InMemoryObserver.Create();
/// // ... run code under test with observer ...
/// observer.SinkLogs.Should().HaveMessage("Order {OrderId} created");
/// observer.InMemoryMeasurer.Should().HaveCounter("orders.created");
/// </code>
/// </example>
public class InMemoryObserver : Observer
{
    /// <summary>The sink holding every captured log event.</summary>
    public readonly InMemorySink SinkLogs;
    /// <summary>The measurer holding every recorded metric.</summary>
    public readonly InMemoryMeasurer InMemoryMeasurer;

    private InMemoryObserver(Logger logger, InMemorySink sinkLogs, InMemoryMeasurer inMemoryMeasurer) :
        base(logger, inMemoryMeasurer)
    {
        SinkLogs = sinkLogs;
        InMemoryMeasurer = inMemoryMeasurer;
    }

    /// <summary>
    /// Creates a new observer with a fresh <see cref="Observability.Monitoring.InMemorySink"/> and <see cref="Observability.Monitoring.InMemoryMeasurer"/>.
    /// </summary>
    /// <returns>A new <see cref="InMemoryObserver"/>.</returns>
    public static InMemoryObserver Create()
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.FromLogContext()
            .WriteTo.InMemory(out var sinkLogs)
            .CreateLogger();

        return new InMemoryObserver(logger, sinkLogs, new InMemoryMeasurer());
    }
}
