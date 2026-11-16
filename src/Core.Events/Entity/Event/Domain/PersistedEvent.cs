using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Domain;

/// <summary>
/// Generic persisted event envelope replacing the domain-specific <c>PersistedDomainEvent</c>/
/// <c>PersistedTaskEvent</c>. Adds the storage identity from <see cref="IPersistedEntity"/>.
/// </summary>
/// <param name="id">The storage-assigned identifier.</param>
/// <param name="typeAggregate">The aggregate type identifier. Required.</param>
/// <param name="aggregateId">The identifier of the aggregate instance. Required.</param>
/// <param name="typeEvent">The event type identifier. Required.</param>
/// <param name="payload">The event payload: a JSON string, an object, or <see langword="null"/>.</param>
/// <param name="moment">When the event happened. Defaults to the current UTC time.</param>
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
    /// <inheritdoc/>
    public long Id { get; } = id;

    /// <summary>
    /// Creates a persisted event from strongly-typed aggregate and event enums, converting them to
    /// their description strings. C# constructors cannot be generic, so this is exposed as a static
    /// factory equivalent to the requested generic enum constructor overload.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type enum.</typeparam>
    /// <typeparam name="TEvent">The event type enum.</typeparam>
    /// <param name="id">The storage-assigned identifier.</param>
    /// <param name="typeAggregate">The aggregate type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="aggregateId">The identifier of the aggregate instance.</param>
    /// <param name="typeEvent">The event type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="payload">The event payload: a JSON string, an object, or <see langword="null"/>.</param>
    /// <param name="moment">When the event happened. Defaults to the current UTC time.</param>
    /// <returns>The new persisted event.</returns>
    /// <exception cref="Core.Extension.Exceptions.MissingEnumDescriptionException">
    /// An enum value has no description.
    /// </exception>
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

    /// <inheritdoc/>
    /// <remarks>The payload reference is shared with the clone, not deep-copied.</remarks>
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
