using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Dispatches an event through an ordered collection of handlers, accumulating the resulting state.
/// </summary>
public class EventHandlerBus<T>(ImmutableList<IDomainEventHandler<T>> collectionHandler) where T : IEntity
{
    public virtual async Task<T> ProcessAsync(Event domainEvent, T? entityState,
                                             CancellationToken? cancellationToken = null)
    {
        var wasHandled = false;

        foreach (var handler in collectionHandler)
        {
            try
            {
                if (!handler.CanHandleEvent(domainEvent, entityState))
                {
                    continue;
                }

                wasHandled = true;
                entityState = await handler.HandleAsync(domainEvent, entityState, cancellationToken);
            }
            catch (NoStateFoundForOptionalEventHandlerException)
            {
                wasHandled = true;
            }
            catch (Exception ex)
            {
                throw new EventHandlerExecutionException(domainEvent.TypeEvent, handler.GetType(), ex);
            }
        }

        if (!wasHandled)
        {
            throw new NoHandlersForEventException(domainEvent.TypeEvent);
        }

        return entityState ?? throw new EmptyStateAfterHandlerBusException();
    }
}
