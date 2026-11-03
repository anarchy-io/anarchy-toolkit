using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Core.Observability.TestHelpers;

public class InMemorySink : ILogEventSink
{
    public IReadOnlyCollection<LogEvent> Events => _events;
    public void Emit(LogEvent logEvent) => _events.Add(logEvent);
    private readonly List<LogEvent> _events = new List<LogEvent>();
}
