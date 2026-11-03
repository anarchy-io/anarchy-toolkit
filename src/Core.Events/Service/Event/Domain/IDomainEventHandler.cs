using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Contract for a handler capable of processing an <see cref="Event"/> against a piece of state.
/// </summary>
public interface IDomainEventHandler<T> where T : IEntity
{
    bool CanHandleEvent(Event domainEvent, T? modelEntity);
    Task<T> HandleAsync(Event domainEvent, T? modelEntity, CancellationToken? cancellationToken = null);
}
