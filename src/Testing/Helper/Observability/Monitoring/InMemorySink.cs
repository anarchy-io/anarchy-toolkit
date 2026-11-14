using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;

public class InMemorySink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _messages = new();

    public IReadOnlyCollection<LogEvent> Messages => _messages;

    public LogEvent? Last => Messages.LastOrDefault();

    public void Emit(LogEvent logEvent)
    {
        // Materialize the properties to avoid issues with deferred rendering.
        // In some cases, for example Anarchy.Utilities.RequestResponseLogging.AspNetCore, we don't
        // immediately materialize the log event properties. Combined with fingers crossed logging where we
        // first collect them in a buffer and only if certain conditions are met, we propagate the
        // log event to the underlying (serilog) sink. Only in the underlying sink the properties are
        // then materialized.
        MaterializeProperties(logEvent);

        _messages.Enqueue(logEvent);
    }

    private static void MaterializeProperties(LogEvent logEvent)
    {
        using var writer = TextWriter.Null;

        foreach (var property in logEvent.Properties.Values)
            property.Render(writer);
    }
}
