using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Envelopes;

public class EventEnvelopeMessageMapperTest
{
    [Fact]
    public void ToOutboundMessage_WhenRetriedWithSameId_UsesStableIdentityAndSubjectPartition()
    {
        var envelope = new PersistedDomainEvent(42L, "order", "order-1", "order-created");

        var first = EventEnvelopeMessageMapper.ToOutboundMessage(envelope, "orders", "publication-1");
        var retry = EventEnvelopeMessageMapper.ToOutboundMessage(envelope, "orders", "publication-1");

        first.Id.Should().Be("publication-1");
        first.DeduplicationId.Should().Be("publication-1");
        first.PartitionKey.Should().Be("order-1");
        first.Destination.Should().Be("orders");
        retry.Should().BeEquivalentTo(first);
    }

    [Fact]
    public void ToOutboundMessage_WithOverrides_UsesExplicitPartitionAndDeduplicationId()
    {
        var envelope = new DomainEventEnvelope("order", "order-1", "order-created");

        var outbound = EventEnvelopeMessageMapper.ToOutboundMessage(envelope, "order-feed", "publication-1",
            partitionKey: "customer-1", deduplicationId: "publication-group-1");

        outbound.Id.Should().Be("publication-1");
        outbound.PartitionKey.Should().Be("customer-1");
        outbound.DeduplicationId.Should().Be("publication-group-1");
        outbound.Destination.Should().Be("order-feed");
    }

    [Fact]
    public void ToEnvelope_OnRoundTrip_PreservesEventMetadataButNotBrokerMetadata()
    {
        var attributes = new Dictionary<string, string> {
            ["Type"] = "envelope-metadata",
            ["type"] = "different-casing",
            ["correlationid"] = "event-correlation"
        };
        var envelope = new PersistedDomainEvent(42L, "order", "order-1", "order-created", data: "\"ready\"",
            customAttributes: attributes);
        var outbound = EventEnvelopeMessageMapper.ToOutboundMessage(envelope, "order-feed", "publication-1",
            partitionKey: "customer-1", attributes: new Dictionary<string, string> {
                ["Type"] = "message-metadata",
                ["correlationid"] = "message-correlation",
                ["Source"] = "publisher-label"
            });
        var inbound = new InboundMessage("broker-id", "broker-reference", outbound.Message, "broker-receive-location",
            outbound.PartitionKey, outbound.Attributes);

        var roundTrip = EventEnvelopeMessageMapper.ToEnvelope<PersistedDomainEvent>(inbound);

        outbound.Attributes.Should().BeEquivalentTo(new Dictionary<string, string> {
            ["AggregateType"] = "order",
            ["Subject"] = "order-1",
            ["Type"] = "order-created",
            ["type"] = "different-casing",
            ["correlationid"] = "message-correlation",
            ["Source"] = "publisher-label"
        });
        roundTrip.Should().BeEquivalentTo(envelope, options => options.Excluding(x => x.Data));
        roundTrip.CastData<string>().Should().Be("ready");
    }

    [Fact]
    public void ToInboundMessage_AndOutboundMessage_RoundTripCustomAttributes()
    {
        var envelope = new DomainEventEnvelope("order", "order-1", "created", new { Total = 5 },
            customAttributes: new Dictionary<string, string> { ["correlationid"] = "trace-1" });
        var inbound = EventEnvelopeMessageMapper.ToInboundMessage(envelope, "orders", "received-1", "receipt-1");
        var outbound = EventEnvelopeMessageMapper.ToOutboundMessage(envelope, "events", "sent-1");

        inbound.Attributes["correlationid"].Should().Be("trace-1");
        inbound.PartitionKey.Should().Be("order-1");
        EventEnvelopeMessageMapper.ToEnvelope<DomainEventEnvelope>(inbound).CustomAttributes.Should().BeEquivalentTo(envelope.CustomAttributes);
        EventEnvelopeMessageMapper.ToEnvelope<DomainEventEnvelope>(outbound).CustomAttributes.Should().BeEquivalentTo(envelope.CustomAttributes);
    }

    [Fact]
    public void ToEnvelope_WithAttributesButWithoutAnEventBody_DoesNotFabricateAnEnvelope()
    {
        var inbound = new InboundMessage("id", "reference", "null", "orders",
            attributes: new Dictionary<string, string> {
                ["AggregateType"] = "order",
                ["Subject"] = "order-1",
                ["Type"] = "order-created"
            });

        var act = () => EventEnvelopeMessageMapper.ToEnvelope<DomainEventEnvelope>(inbound);

        act.Should().Throw<EventEnvelopeParsingException>();
    }
}
