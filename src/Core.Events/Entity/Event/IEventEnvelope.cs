using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Entity.Event;

/// <summary>
/// Contract describing an event envelope: the aggregate/event identity plus its payload.
/// </summary>
public interface IEventEnvelope : IEntity
{
    public string TypeAggregate { get; }
    public string AggregateId { get; }
    public string TypeEvent { get; }
    public DateTimeOffset Moment { get; }
    public object? Payload { get; }
}
