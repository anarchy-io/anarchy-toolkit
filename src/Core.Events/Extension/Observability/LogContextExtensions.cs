using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Core.Events.Extension.Observability;

/// <summary>
/// Log-context helpers that enrich structured logs with event envelope properties.
/// </summary>
public static class LogContextExtensions
{
    public static LogContextProperty[] ForEvent(IEventEnvelope domainEvent)
    {
        return [
            new LogContextProperty("AggregateId", domainEvent.AggregateId),
            new LogContextProperty("TypeAggregate", domainEvent.TypeAggregate),
            new LogContextProperty("TypeEvent", domainEvent.TypeEvent)
        ];
    }

    extension(LogContext logContext)
    {
        public IDisposable WithEvent(IEventEnvelope domainEvent)
        {
            return logContext.PushProperties(ForEvent(domainEvent));
        }
    }
}
