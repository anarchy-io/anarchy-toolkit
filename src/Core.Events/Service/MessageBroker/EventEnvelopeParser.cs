using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Events.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Core.Events.Service.MessageBroker;

/// <summary>
/// Deserializes an inbound broker message body into a typed event envelope using Newtonsoft.Json.
/// </summary>
public class EventEnvelopeParser<TInboundMessage, TEventEnvelope> : IEventEnvelopeParser<TInboundMessage, TEventEnvelope>
    where TInboundMessage : InboundMessageEntity where TEventEnvelope : class, IEventEnvelope
{
    public Task<TEventEnvelope> FromInboundMessage(TInboundMessage inboundMessage,
                                                   CancellationToken? cancellationToken = null)
    {
        try
        {
            // Deserializing with Newtonsoft.Json intentionally for relaxed requirements on polymorphic types.
            // Do not change to System.Text.Json without careful consideration on available constructors on
            // the Event / PersistedEvent types
            var result = JsonConvert.DeserializeObject<TEventEnvelope>(inboundMessage.Message)
                         ?? throw new EventEnvelopeParsingException(typeof(TEventEnvelope));
            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new EventEnvelopeParsingException(typeof(TEventEnvelope), ex);
        }
    }
}
