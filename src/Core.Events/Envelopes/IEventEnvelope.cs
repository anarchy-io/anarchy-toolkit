using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Contract describing an event envelope: the aggregate/event identity, data and custom metadata.
/// </summary>
/// <remarks>
/// Identity values are plain strings (typically enum descriptions) so the event pipeline stays decoupled from
/// domain-specific enums. See <see cref="EventEnvelopeBase{TPayload}"/> for the base implementation.
/// </remarks>
public interface IEventEnvelope : IEntity
{
    /// <summary>
    /// Gets the aggregate type identifier (e.g. <c>"order"</c>).
    /// </summary>
    public string AggregateType { get; }

    /// <summary>
    /// Gets the identifier of the aggregate instance the event belongs to.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Gets the event type identifier (e.g. <c>"order-created"</c>).
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets when the event happened.
    /// </summary>
    public DateTimeOffset Time { get; }

    /// <summary>
    /// Gets the untyped event payload, or <see langword="null"/> when the event carries none.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Gets the event's custom string attributes, or an empty dictionary when none were supplied.
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomAttributes { get; }
}
