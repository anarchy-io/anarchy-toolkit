using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

namespace Anarchy.Toolkit.Core.Events.Service.MessageBroker;

/// <summary>
/// Parses an inbound broker message into a typed event envelope.
/// </summary>
public interface IEventEnvelopeParser<in TInboundMessage, TEventEnvelope>
    where TInboundMessage : InboundMessageEntity where TEventEnvelope : class?, IEventEnvelope?
{
    public Task<TEventEnvelope> FromInboundMessage(TInboundMessage inboundMessage,
                                                   CancellationToken? cancellationToken = null);
}
