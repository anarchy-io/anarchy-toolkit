using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

namespace Anarchy.Toolkit.Core.Events.Service.MessageBroker;

/// <summary>
/// Parses an inbound broker message into a typed event envelope.
/// </summary>
/// <remarks>
/// The default implementation is <see cref="EventEnvelopeParser{TInboundMessage, TEventEnvelope}"/>.
/// </remarks>
/// <typeparam name="TInboundMessage">The broker-specific inbound message type.</typeparam>
/// <typeparam name="TEventEnvelope">The envelope type to produce.</typeparam>
public interface IEventEnvelopeParser<in TInboundMessage, TEventEnvelope>
    where TInboundMessage : InboundMessageEntity where TEventEnvelope : class?, IEventEnvelope?
{
    /// <summary>
    /// Parses the body (<see cref="InboundMessageEntity.Message"/>) of <paramref name="inboundMessage"/> into an
    /// event envelope.
    /// </summary>
    /// <param name="inboundMessage">The message received from the broker.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The parsed envelope.</returns>
    public Task<TEventEnvelope> FromInboundMessage(TInboundMessage inboundMessage,
                                                   CancellationToken? cancellationToken = null);
}
