using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using CloudNative.CloudEvents;

namespace Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;

/// <summary>Maps Anarchy event envelopes and structured broker message bodies to and from CloudEvents.</summary>
public interface ICloudEventEnvelopeMapper
{
    /// <summary>Creates a CloudEvent using a retained event ID or generates a UUIDv7 if none is retained.</summary>
    CloudEvent ToCloudEvent(IEventEnvelope envelope, Uri? source = null,
                           string? dataContentType = null, Uri? dataSchema = null);

    /// <summary>Creates a CloudEvent with an explicit event ID; a supplied source overrides the configured source.</summary>
    CloudEvent ToCloudEvent(IEventEnvelope envelope, string id, Uri? source = null,
                           string? dataContentType = null, Uri? dataSchema = null);

    /// <summary>Creates an envelope, retaining additional CloudEvent context in namespaced custom attributes.</summary>
    DomainEventEnvelope ToEnvelope(CloudEvent cloudEvent);

    /// <summary>Decodes the inbound structured JSON body; broker routing and attributes stay on the message.</summary>
    CloudEvent ToCloudEvent(InboundMessage message);

    /// <summary>Decodes the outbound structured JSON body; broker routing and attributes stay on the message.</summary>
    CloudEvent ToCloudEvent(OutboundMessage message);

    /// <summary>Encodes a structured JSON body with explicit broker delivery metadata.</summary>
    InboundMessage ToInboundMessage(CloudEvent cloudEvent, string messageId, string brokerSource, string brokerReference,
                                   string? partitionKey = null, IReadOnlyDictionary<string, string>? attributes = null);

    /// <summary>Encodes a structured JSON body with explicit broker publication metadata.</summary>
    OutboundMessage ToOutboundMessage(CloudEvent cloudEvent, string messageId, string destination,
                                     string? partitionKey = null, string? deduplicationId = null,
                                     IReadOnlyDictionary<string, string>? attributes = null);
}
