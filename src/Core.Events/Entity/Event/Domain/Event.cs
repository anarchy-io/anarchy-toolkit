using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Extension;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Domain;

/// <summary>
/// Generic concrete event envelope replacing the domain-specific <c>DomainEvent</c>/<c>TaskEvent</c>.
/// The aggregate and event types are stored as strings so the pipeline stays decoupled from any
/// concrete enum. Convenience constructor overloads accept arbitrary <see cref="Enum"/> values and
/// convert them to their description strings.
/// </summary>
[method: JsonConstructor]
public class Event(
    string typeAggregate,
    string aggregateId,
    string typeEvent,
    object? payload = null,
    DateTimeOffset? moment = null)
    : EventEnvelopeBase<IEventEnvelopePayload>(typeAggregate, aggregateId, typeEvent, payload, moment),
      IDeepCloneableEntity<Event>
{
    /// <summary>
    /// Creates an event from strongly-typed aggregate and event enums, converting them to their
    /// description strings. C# constructors cannot be generic, so this is exposed as a static
    /// factory equivalent to the requested generic enum constructor overload.
    /// </summary>
    public static Event Create<TAggregate, TEvent>(
        TAggregate typeAggregate,
        string aggregateId,
        TEvent typeEvent,
        object? payload = null,
        DateTimeOffset? moment = null)
        where TAggregate : Enum
        where TEvent : Enum
        => new(typeAggregate.ToDescription(), aggregateId, typeEvent.ToDescription(), payload, moment);

    public override Event DeepClone()
    {
        // Clone from raw strings: the concrete enum types are unknown at this level.
        return new Event(
            TypeAggregate,
            AggregateId,
            TypeEvent,
            Payload,
            Moment);
    }
}
