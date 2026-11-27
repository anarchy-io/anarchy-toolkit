using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Extensions.System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Generic persisted event envelope replacing the former domain-specific
/// persisted event types. Adds the storage identity from <see cref="IPersistedEntity"/>.
/// </summary>
/// <param name="id">The storage-assigned identifier.</param>
/// <param name="aggregateType">The aggregate type identifier. Required.</param>
/// <param name="subject">The identifier of the aggregate instance. Required.</param>
/// <param name="type">The event type identifier. Required.</param>
/// <param name="data">The event data: a JSON string, an object, or <see langword="null"/>.</param>
/// <param name="time">When the event happened. Defaults to the current UTC time.</param>
/// <param name="customAttributes">Optional event metadata. The envelope stores a read-only snapshot.</param>
public class PersistedDomainEvent(
    long id,
    string aggregateType,
    string subject,
    string type,
    object? data = null,
    DateTimeOffset? time = null,
    IReadOnlyDictionary<string, string>? customAttributes = null)
    : DomainEventEnvelope(aggregateType, subject, type, data, time, customAttributes),
      IDeepCloneableEntity<PersistedDomainEvent>, IPersistedEventEnvelope
{
    [JsonConstructor]
    private PersistedDomainEvent(
        long id,
        string aggregateType,
        string subject,
        string type,
        JToken? data,
        DateTimeOffset? time,
        IReadOnlyDictionary<string, string>? customAttributes)
        : this(id, aggregateType, subject, type, (object?)data, time, customAttributes)
    {
    }

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
    /// <param name="aggregateType">The aggregate type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="subject">The identifier of the aggregate instance.</param>
    /// <param name="type">The event type; must have a <see cref="System.ComponentModel.DescriptionAttribute"/>.</param>
    /// <param name="data">The event data: a JSON string, an object, or <see langword="null"/>.</param>
    /// <param name="time">When the event happened. Defaults to the current UTC time.</param>
    /// <param name="customAttributes">Optional event metadata. The envelope stores a read-only snapshot.</param>
    /// <returns>The new persisted event.</returns>
    /// <exception cref="Core.Exceptions.MissingEnumDescriptionException">
    /// An enum value has no description.
    /// </exception>
    public static PersistedDomainEvent Create<TAggregate, TEvent>(
        long id,
        TAggregate aggregateType,
        string subject,
        TEvent type,
        object? data = null,
        DateTimeOffset? time = null,
        IReadOnlyDictionary<string, string>? customAttributes = null)
        where TAggregate : Enum
        where TEvent : Enum
        => new(id, aggregateType.ToDescription(), subject, type.ToDescription(), data, time, customAttributes);

    /// <inheritdoc/>
    /// <remarks>The payload reference is shared with the clone, not deep-copied.</remarks>
    public override PersistedDomainEvent DeepClone()
    {
        // Clone from raw strings: the concrete enum types are unknown at this level.
        return new PersistedDomainEvent(
            Id,
            AggregateType,
            Subject,
            Type,
            Data,
            Time,
            CustomAttributes);
    }
}
