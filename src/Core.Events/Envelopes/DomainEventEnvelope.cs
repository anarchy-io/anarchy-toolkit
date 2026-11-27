using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Extensions.System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Generic concrete event envelope replacing the domain-specific <c>DomainEvent</c>.
/// The aggregate and event types are stored as strings so the pipeline stays decoupled from any
/// concrete enum. Convenience constructor overloads accept arbitrary <see cref="Enum"/> values and
/// convert them to their description strings.
/// </summary>
/// <remarks>
/// This is the event type consumed by <see cref="Domain.IDomainEventHandler{T}"/>,
/// <see cref="Domain.DomainEventHandlerDispatcher{T}"/> and <see cref="Domain.DomainEventProcessorBase{T}"/>.
/// </remarks>
/// <param name="aggregateType">The aggregate type identifier. Required.</param>
/// <param name="subject">The identifier of the aggregate instance. Required.</param>
/// <param name="type">The event type identifier. Required.</param>
/// <param name="data">The event data: a JSON string, an object, or <see langword="null"/>.</param>
/// <param name="time">When the event happened. Defaults to the current UTC time.</param>
/// <param name="customAttributes">Optional event metadata. The envelope stores a read-only snapshot.</param>
/// <example>
/// <code language="csharp">
/// var created = DomainEventEnvelope.Create(Aggregate.Order, orderId, OrderEvent.Created, new { Total = 10m });
/// var raw = new DomainEventEnvelope("order", orderId, "order-created", "{\"Total\":10}");
/// </code>
/// </example>
public class DomainEventEnvelope(
    string aggregateType,
    string subject,
    string type,
    object? data = null,
    DateTimeOffset? time = null,
    IReadOnlyDictionary<string, string>? customAttributes = null)
    : EventEnvelopeBase<IEventEnvelopePayload>(aggregateType, subject, type, data, time, customAttributes),
      IDeepCloneableEntity<DomainEventEnvelope>
{
    // Read a string value as a JSON token so it is not mistaken for raw JSON by the public constructor.
    [JsonConstructor]
    private DomainEventEnvelope(
        string aggregateType,
        string subject,
        string type,
        JToken? data,
        DateTimeOffset? time,
        IReadOnlyDictionary<string, string>? customAttributes)
        : this(aggregateType, subject, type, (object?)data, time, customAttributes)
    {
    }

    /// <summary>
    /// Creates an event from strongly-typed aggregate and event enums, converting them to their
    /// description strings. C# constructors cannot be generic, so this is exposed as a static
    /// factory equivalent to the requested generic enum constructor overload.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type enum.</typeparam>
    /// <typeparam name="TEvent">The event type enum.</typeparam>
    /// <param name="aggregateType">The aggregate type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="subject">The identifier of the aggregate instance.</param>
    /// <param name="type">The event type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="data">The event data: a JSON string, an object, or <see langword="null"/>.</param>
    /// <param name="time">When the event happened. Defaults to the current UTC time.</param>
    /// <param name="customAttributes">Optional event metadata. The envelope stores a read-only snapshot.</param>
    /// <returns>The new event.</returns>
    /// <exception cref="Core.Exceptions.MissingEnumDescriptionException">
    /// An enum value has no description.
    /// </exception>
    public static DomainEventEnvelope Create<TAggregate, TEvent>(
        TAggregate aggregateType,
        string subject,
        TEvent type,
        object? data = null,
        DateTimeOffset? time = null,
        IReadOnlyDictionary<string, string>? customAttributes = null)
        where TAggregate : Enum
        where TEvent : Enum
        => new(aggregateType.ToDescription(), subject, type.ToDescription(), data, time, customAttributes);

    /// <inheritdoc/>
    /// <remarks>The payload reference is shared with the clone, not deep-copied.</remarks>
    public override DomainEventEnvelope DeepClone()
    {
        // Clone from raw strings: the concrete enum types are unknown at this level.
        return new DomainEventEnvelope(
            AggregateType,
            Subject,
            Type,
            Data,
            Time,
            CustomAttributes);
    }
}
