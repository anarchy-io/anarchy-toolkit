using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Contract describing an event envelope: the aggregate/event identity plus its payload.
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
    public string TypeAggregate { get; }

    /// <summary>
    /// Gets the identifier of the aggregate instance the event belongs to.
    /// </summary>
    public string AggregateId { get; }

    /// <summary>
    /// Gets the event type identifier (e.g. <c>"order-created"</c>).
    /// </summary>
    public string TypeEvent { get; }

    /// <summary>
    /// Gets when the event happened.
    /// </summary>
    public DateTimeOffset Moment { get; }

    /// <summary>
    /// Gets the untyped event payload, or <see langword="null"/> when the event carries none.
    /// </summary>
    public object? Payload { get; }
}
