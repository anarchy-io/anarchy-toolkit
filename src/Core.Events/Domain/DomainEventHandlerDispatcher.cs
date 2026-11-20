using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Domain.Exceptions;
using Anarchy.Toolkit.Core.Events.Envelopes;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Core.Events.Domain;

/// <summary>
/// Dispatches an event through an ordered collection of handlers, accumulating the resulting state.
/// </summary>
/// <remarks>
/// Handlers run sequentially in list order; each matching handler receives the state returned by the previous one,
/// so the order of <paramref name="collectionHandler"/> matters (e.g. state-creation handlers before mutation
/// handlers).
/// </remarks>
/// <typeparam name="T">The aggregate state type.</typeparam>
/// <param name="collectionHandler">The ordered handlers to dispatch to.</param>
public class DomainEventHandlerDispatcher<T>(ImmutableList<IDomainEventHandler<T>> collectionHandler) where T : IEntity
{
    /// <summary>
    /// Runs every handler whose <see cref="IDomainEventHandler{T}.CanHandleEvent"/> returns <see langword="true"/>,
    /// threading the state through them.
    /// </summary>
    /// <remarks>
    /// A <see cref="NoStateFoundForOptionalEventHandlerException"/> from a handler counts as "handled" and leaves the
    /// state unchanged.
    /// </remarks>
    /// <param name="domainEvent">The event to dispatch.</param>
    /// <param name="entityState">The current state, or <see langword="null"/> when none exists yet.</param>
    /// <param name="cancellationToken">Optional cancellation token forwarded to the handlers.</param>
    /// <returns>The state after all matching handlers have run.</returns>
    /// <exception cref="EventHandlerExecutionException">A handler threw; the original failure is the inner exception.</exception>
    /// <exception cref="NoHandlersForEventException">No handler accepted the event.</exception>
    /// <exception cref="EmptyStateAfterHandlerDispatchException">The state is still <see langword="null"/> after all handlers ran.</exception>
    public virtual async Task<T> ProcessAsync(DomainEventEnvelope domainEvent, T? entityState,
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

        return entityState ?? throw new EmptyStateAfterHandlerDispatchException();
    }
}
