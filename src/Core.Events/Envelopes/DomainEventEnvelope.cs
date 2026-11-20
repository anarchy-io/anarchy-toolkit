using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Extension;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Generic concrete event envelope replacing the domain-specific <c>DomainEvent</c>/<c>TaskEvent</c>.
/// The aggregate and event types are stored as strings so the pipeline stays decoupled from any
/// concrete enum. Convenience constructor overloads accept arbitrary <see cref="Enum"/> values and
/// convert them to their description strings.
/// </summary>
/// <remarks>
/// This is the event type consumed by <see cref="Domain.IDomainEventHandler{T}"/>,
/// <see cref="Domain.DomainEventHandlerDispatcher{T}"/> and <see cref="Domain.DomainEventProcessorBase{T}"/>.
/// The primary constructor is the Newtonsoft.Json deserialization constructor.
/// </remarks>
/// <param name="typeAggregate">The aggregate type identifier. Required.</param>
/// <param name="aggregateId">The identifier of the aggregate instance. Required.</param>
/// <param name="typeEvent">The event type identifier. Required.</param>
/// <param name="payload">The event payload: a JSON string, an object, or <see langword="null"/>.</param>
/// <param name="moment">When the event happened. Defaults to the current UTC time.</param>
/// <example>
/// <code language="csharp">
/// var created = DomainEventEnvelope.Create(Aggregate.Order, orderId, OrderEvent.Created, new { Total = 10m });
/// var raw = new DomainEventEnvelope("order", orderId, "order-created", "{\"Total\":10}");
/// </code>
/// </example>
[method: JsonConstructor]
public class DomainEventEnvelope(
    string typeAggregate,
    string aggregateId,
    string typeEvent,
    object? payload = null,
    DateTimeOffset? moment = null)
    : EventEnvelopeBase<IEventEnvelopePayload>(typeAggregate, aggregateId, typeEvent, payload, moment),
      IDeepCloneableEntity<DomainEventEnvelope>
{
    /// <summary>
    /// Creates an event from strongly-typed aggregate and event enums, converting them to their
    /// description strings. C# constructors cannot be generic, so this is exposed as a static
    /// factory equivalent to the requested generic enum constructor overload.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type enum.</typeparam>
    /// <typeparam name="TEvent">The event type enum.</typeparam>
    /// <param name="typeAggregate">The aggregate type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="aggregateId">The identifier of the aggregate instance.</param>
    /// <param name="typeEvent">The event type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="payload">The event payload: a JSON string, an object, or <see langword="null"/>.</param>
    /// <param name="moment">When the event happened. Defaults to the current UTC time.</param>
    /// <returns>The new event.</returns>
    /// <exception cref="Core.Extension.Exceptions.MissingEnumDescriptionException">
    /// An enum value has no description.
    /// </exception>
    public static DomainEventEnvelope Create<TAggregate, TEvent>(
        TAggregate typeAggregate,
        string aggregateId,
        TEvent typeEvent,
        object? payload = null,
        DateTimeOffset? moment = null)
        where TAggregate : Enum
        where TEvent : Enum
        => new(typeAggregate.ToDescription(), aggregateId, typeEvent.ToDescription(), payload, moment);

    /// <inheritdoc/>
    /// <remarks>The payload reference is shared with the clone, not deep-copied.</remarks>
    public override DomainEventEnvelope DeepClone()
    {
        // Clone from raw strings: the concrete enum types are unknown at this level.
        return new DomainEventEnvelope(
            TypeAggregate,
            AggregateId,
            TypeEvent,
            Payload,
            Moment);
    }
}
