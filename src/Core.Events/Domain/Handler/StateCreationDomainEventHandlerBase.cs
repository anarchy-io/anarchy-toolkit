using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Domain.Handler;

/// <summary>
/// Base class for handlers that create (initialize) aggregate state from an event.
/// </summary>
/// <remarks>
/// The handler is considered for supported events whether or not state already exists; implementations receive the
/// current state (possibly <see langword="null"/>) in
/// <see cref="DomainEventHandlerBase{TDomainModelEntity}.HandleAsync"/> and return the initialized state.
/// Implementations may throw <see cref="Exceptions.StateAlreadyInitializedEventHandlerException"/> when state must
/// not exist yet.
/// </remarks>
/// <typeparam name="TDomainModelEntity">The aggregate state type.</typeparam>
/// <param name="supportedAggregateTypes">The aggregate/event types this handler supports.</param>
/// <example>
/// <code language="csharp">
/// public sealed class OrderCreatedHandler()
///     : StateCreationDomainEventHandlerBase&lt;Order&gt;(Supports(Aggregate.Order, OrderEvent.Created))
/// {
///     public override Task&lt;Order&gt; HandleAsync(DomainEventEnvelope domainEvent, Order? entity,
///                                              CancellationToken? cancellationToken = null)
///         =&gt; Task.FromResult(new Order(domainEvent.Subject, domainEvent.CastData&lt;OrderCreated&gt;()));
/// }
/// </code>
/// </example>
public abstract class StateCreationDomainEventHandlerBase<TDomainModelEntity>(
    DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes
        supportedAggregateTypes)
    : DomainEventHandlerBase<TDomainModelEntity>(supportedAggregateTypes, isStateInitializer: true)
    where TDomainModelEntity : IEntity;
