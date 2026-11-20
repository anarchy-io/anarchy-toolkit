using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Envelopes;

namespace Anarchy.Toolkit.Core.Events.Domain;

/// <summary>
/// Contract for a handler capable of processing an <see cref="DomainEventEnvelope"/> against a piece of state.
/// </summary>
/// <remarks>
/// Handlers are composed in an <see cref="DomainEventHandlerDispatcher{T}"/>, which calls <see cref="CanHandleEvent"/> and then
/// <see cref="HandleAsync"/> on every matching handler. Most implementations derive from
/// <see cref="Handler.DomainEventHandlerBase{TDomainModelEntity}"/>.
/// </remarks>
/// <typeparam name="T">The aggregate state type.</typeparam>
public interface IDomainEventHandler<T> where T : IEntity
{
    /// <summary>
    /// Determines whether this handler should process <paramref name="domainEvent"/> given the current state.
    /// </summary>
    /// <param name="domainEvent">The incoming event.</param>
    /// <param name="modelEntity">The current aggregate state, or <see langword="null"/> when none exists yet.</param>
    /// <returns><see langword="true"/> when <see cref="HandleAsync"/> should be invoked.</returns>
    bool CanHandleEvent(DomainEventEnvelope domainEvent, T? modelEntity);

    /// <summary>
    /// Applies <paramref name="domainEvent"/> to the aggregate state.
    /// </summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <param name="modelEntity">The current aggregate state, or <see langword="null"/> when none exists yet.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The resulting aggregate state, passed on to the next handler in the bus.</returns>
    Task<T> HandleAsync(DomainEventEnvelope domainEvent, T? modelEntity, CancellationToken? cancellationToken = null);
}
