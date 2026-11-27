using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Domain.Exceptions;
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Events.Extensions.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Core.Events.Domain;

/// <summary>
/// Orchestrates processing of a batch of events: hydrates the affected state, dispatches each event
/// through the <see cref="DomainEventHandlerDispatcher{T}"/>, and persists the resulting state.
/// </summary>
/// <remarks>
/// <para>
/// Lifecycle of <see cref="ExecuteAsync"/>: (1) <see cref="HydrateStateEntitiesAsync"/> loads the current state of
/// every aggregate referenced by the batch; (2) events are processed sequentially in batch order, each inside a log
/// context carrying the event identity; (3) after each successfully handled event the new state is persisted through
/// <see cref="StoreStateEntity"/> and becomes the input for the next event of the same aggregate.
/// </para>
/// <para>
/// When the bus throws <see cref="EmptyStateAfterHandlerDispatchException"/>, a warning and the
/// <c>empty_state_after_handler_bus</c> counter are emitted and the event is skipped. When it throws
/// <see cref="NoHandlersForEventException"/>, a warning and a counter are emitted and the exception is rethrown,
/// aborting the batch. Any other exception propagates.
/// </para>
/// </remarks>
/// <typeparam name="T">The aggregate state type.</typeparam>
/// <param name="domainEventHandlerDispatcher">The bus that applies events to state.</param>
/// <param name="telemetry">The application telemetry used for logging and metrics.</param>
/// <example>
/// <code language="csharp">
/// public sealed class OrderEventProcessor(DomainEventHandlerDispatcher&lt;Order&gt; bus, AppTelemetry telemetry, IOrderRepository repository)
///     : DomainEventProcessorBase&lt;Order&gt;(bus, telemetry)
/// {
///     protected override async Task&lt;Dictionary&lt;string, Order&gt;&gt; HydrateStateEntitiesAsync(
///         string[] aggregateIds, CancellationToken? cancellationToken = null)
///         =&gt; (await repository.GetByIdsAsync(aggregateIds)).ToDictionary(o =&gt; o.Id);
///
///     protected override Task&lt;Order&gt; StoreStateEntity(Order updatedStateEntity,
///                                                   CancellationToken? cancellationToken = null)
///         =&gt; repository.SaveAsync(updatedStateEntity);
/// }
/// </code>
/// </example>
public abstract class DomainEventProcessorBase<T>(DomainEventHandlerDispatcher<T> domainEventHandlerDispatcher, AppTelemetry telemetry)
    where T : IEntity
{
    /// <summary>
    /// The application telemetry used for logging and metrics.
    /// </summary>
    protected readonly AppTelemetry Telemetry = telemetry;

    /// <summary>
    /// Processes <paramref name="domainEvents"/> in order and persists the resulting state of each affected aggregate.
    /// </summary>
    /// <param name="domainEvents">The events to process, in the order they must be applied.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// The final state of every hydrated or updated aggregate (including hydrated aggregates whose events were
    /// skipped).
    /// </returns>
    /// <exception cref="NoHandlersForEventException">An event has no matching handler.</exception>
    /// <exception cref="EventHandlerExecutionException">A handler failed.</exception>
    public async Task<ImmutableArray<T>> ExecuteAsync(ImmutableArray<DomainEventEnvelope> domainEvents,
                                                      CancellationToken? cancellationToken = null)
    {
        var stateEntities = await HydrateStateEntitiesAsync(domainEvents.Select(x => x.Subject).ToArray(),
                                                            cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            using (Telemetry.WithEvent(domainEvent))
            {
                stateEntities.TryGetValue(domainEvent.Subject, out var currentState);
                var handledEventState = await HandleEventAsync(domainEvent, currentState, cancellationToken);

                if (handledEventState is null)
                {
                    continue;
                }

                stateEntities[domainEvent.Subject] = await StoreStateEntity(handledEventState, cancellationToken);
            }
        }

        return [.. stateEntities.Values];
    }

    /// <summary>
    /// Loads the current state of the given aggregates.
    /// </summary>
    /// <param name="aggregateIds">
    /// The aggregate identifiers referenced by the batch, in event order (may contain duplicates).
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A mutable dictionary keyed by aggregate id; aggregates without state should simply be absent. The processor
    /// updates this dictionary as events are applied.
    /// </returns>
    protected abstract Task<Dictionary<string, T>> HydrateStateEntitiesAsync(string[] aggregateIds,
                                                                             CancellationToken? cancellationToken = null);

    /// <summary>
    /// Persists the state produced by handling an event.
    /// </summary>
    /// <param name="updatedStateEntity">The state to persist.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The persisted state (e.g. with storage-assigned values), used for subsequent events.</returns>
    protected abstract Task<T> StoreStateEntity(T updatedStateEntity, CancellationToken? cancellationToken = null);

    private async Task<T?> HandleEventAsync(DomainEventEnvelope domainEvent, T? entityState, CancellationToken? cancellationToken)
    {
        var caller = this.CalledFrom();

        try
        {
            return await domainEventHandlerDispatcher.ProcessAsync(domainEvent, entityState, cancellationToken);
        }
        catch (EmptyStateAfterHandlerDispatchException)
        {
            Telemetry.Log(LogLevel.Warning, caller, null, "No states were altered after processing event");
            Telemetry.IncrementCounter(caller, "empty_state_after_handler_bus", 1, [new("success", false)]);

            return default;
        }
        catch (NoHandlersForEventException)
        {
            Telemetry.Log(LogLevel.Warning, caller, null, "Event has no matching handler");
            Telemetry.IncrementCounter(caller, MetricSuffixes.NoHandlers, 1,
                new("success", false), new("entity_state", entityState is null ? "empty" : "found"));

            throw;
        }
    }
}
