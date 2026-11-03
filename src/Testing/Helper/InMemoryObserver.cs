using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper;

public class InMemoryObserver : Observer
{
    public readonly InMemorySink SinkLogs;
    public readonly InMemoryMeasurer InMemoryMeasurer;

    private InMemoryObserver(Logger logger, InMemorySink sinkLogs, InMemoryMeasurer inMemoryMeasurer) :
        base(logger, inMemoryMeasurer)
    {
        SinkLogs = sinkLogs;
        InMemoryMeasurer = inMemoryMeasurer;
    }

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
