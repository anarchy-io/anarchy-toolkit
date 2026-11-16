using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Base class for handlers that mutate existing aggregate state. Requires the state to be present.
/// </summary>
/// <remarks>
/// <see cref="DomainEventHandlerBase{TDomainModelEntity}.CanHandleEvent"/> returns <see langword="false"/> when no
/// state exists, so the <see cref="EventHandlerBus{T}"/> skips this handler for aggregates that have not been
/// initialized yet. Implement the protected
/// <see cref="HandleAsync(TDomainModelEntity, Event, CancellationToken?)"/> overload, which receives non-null state.
/// </remarks>
/// <typeparam name="TDomainModelEntity">The aggregate state type.</typeparam>
/// <param name="supportedAggregateTypes">The aggregate/event types this handler supports.</param>
/// <example>
/// <code language="csharp">
/// public sealed class OrderPaidHandler()
///     : StateMutationDomainEventHandlerBase&lt;Order&gt;(Supports(Aggregate.Order, OrderEvent.Paid))
/// {
///     protected override Task&lt;Order&gt; HandleAsync(Order order, Event domainEvent,
///                                                CancellationToken? cancellationToken = null)
///         =&gt; Task.FromResult(order with { IsPaid = true });
/// }
/// </code>
/// </example>
public abstract class StateMutationDomainEventHandlerBase<TDomainModelEntity>(
    DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes
        supportedAggregateTypes)
    : DomainEventHandlerBase<TDomainModelEntity>(supportedAggregateTypes, isStateInitializer: false)
    where TDomainModelEntity : IEntity
{
    /// <summary>
    /// Validates that state exists and delegates to
    /// <see cref="HandleAsync(TDomainModelEntity, Event, CancellationToken?)"/>.
    /// </summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <param name="modelEntity">The current aggregate state; must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated aggregate state.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="modelEntity"/> is <see langword="null"/>.</exception>
    public override async Task<TDomainModelEntity> HandleAsync(Event domainEvent,
                                                               TDomainModelEntity? modelEntity,
                                                               CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(modelEntity);

        return await HandleAsync(modelEntity, domainEvent, cancellationToken);
    }

    /// <summary>
    /// Applies <paramref name="domainEvent"/> to the existing aggregate state.
    /// </summary>
    /// <param name="modelEntity">The current, non-null aggregate state.</param>
    /// <param name="domainEvent">The event to apply.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated state.</returns>
    protected abstract Task<TDomainModelEntity> HandleAsync(TDomainModelEntity modelEntity, Event domainEvent,
                                                            CancellationToken? cancellationToken = null);
}
