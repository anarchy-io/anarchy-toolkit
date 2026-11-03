using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Domain;

/// <summary>
/// Generic persisted event envelope replacing the domain-specific <c>PersistedDomainEvent</c>/
/// <c>PersistedTaskEvent</c>. Adds the storage identity from <see cref="IPersistedEntity"/>.
/// </summary>
public class PersistedEvent(
    long id,
    string typeAggregate,
    string aggregateId,
    string typeEvent,
    object? payload = null,
    DateTimeOffset? moment = null)
    : Event(typeAggregate, aggregateId, typeEvent, payload, moment),
      IDeepCloneableEntity<PersistedEvent>, IPersistedEventEnvelope
{
    public long Id { get; } = id;

    /// <summary>
    /// Creates a persisted event from strongly-typed aggregate and event enums, converting them to
    /// their description strings. C# constructors cannot be generic, so this is exposed as a static
    /// factory equivalent to the requested generic enum constructor overload.
    /// </summary>
    public static PersistedEvent Create<TAggregate, TEvent>(
        long id,
        TAggregate typeAggregate,
        string aggregateId,
        TEvent typeEvent,
        object? payload = null,
        DateTimeOffset? moment = null)
        where TAggregate : Enum
        where TEvent : Enum
        => new(id, typeAggregate.ToDescription(), aggregateId, typeEvent.ToDescription(), payload, moment);

    public override PersistedEvent DeepClone()
    {
        // Clone from raw strings: the concrete enum types are unknown at this level.
        return new PersistedEvent(
            Id,
            TypeAggregate,
            AggregateId,
            TypeEvent,
            Payload,
            Moment);
    }
}
