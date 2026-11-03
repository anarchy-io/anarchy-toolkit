using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Extension.Observability;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Orchestrates processing of a batch of events: hydrates the affected state, dispatches each event
/// through the <see cref="EventHandlerBus{T}"/>, and persists the resulting state.
/// </summary>
public abstract class EventProcessorBase<T>(EventHandlerBus<T> eventHandlerBus, Observer observer)
    where T : IEntity
{
    protected readonly Observer Observer = observer;

    public async Task<ImmutableArray<T>> ExecuteAsync(ImmutableArray<Event> domainEvents,
                                                      CancellationToken? cancellationToken = null)
    {
        var stateEntities = await HydrateStateEntitiesAsync(domainEvents.Select(x => x.AggregateId).ToArray(),
                                                            cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            using (Observer.LogContext.WithEvent(domainEvent))
            {
                stateEntities.TryGetValue(domainEvent.AggregateId, out var currentState);
                var handledEventState = await HandleEventAsync(domainEvent, currentState, cancellationToken);

                if (handledEventState is null)
                {
                    continue;
                }

                stateEntities[domainEvent.AggregateId] = await StoreStateEntity(handledEventState, cancellationToken);
            }
        }

        return [.. stateEntities.Values];
    }

    protected abstract Task<Dictionary<string, T>> HydrateStateEntitiesAsync(string[] aggregateIds,
                                                                             CancellationToken? cancellationToken = null);

    protected abstract Task<T> StoreStateEntity(T updatedStateEntity, CancellationToken? cancellationToken = null);

    private async Task<T?> HandleEventAsync(Event domainEvent, T? entityState, CancellationToken? cancellationToken)
    {
        var caller = this.CalledFrom();

        try
        {
            return await eventHandlerBus.ProcessAsync(domainEvent, entityState, cancellationToken);
        }
        catch (EmptyStateAfterHandlerBusException)
        {
            Observer.LogWarning(caller, "No states were altered after processing event");
            Observer.IncrementCounter(caller, "empty_state_after_handler_bus", 1, Tags.Failure);

            return default;
        }
        catch (NoHandlersForEventException)
        {
            Observer.LogWarning(caller, "Event has no matching handler");
            Observer.IncrementCounter(caller, Observer.MetricSuffixNoHandlers, 1,
                Tags.Failure
                    .WithTag("entity_state", entityState is null ? "empty" : "found"));

            throw;
        }
    }
}
