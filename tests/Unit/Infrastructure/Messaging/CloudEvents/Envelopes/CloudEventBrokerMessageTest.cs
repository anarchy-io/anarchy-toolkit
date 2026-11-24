using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes.Exceptions;
using AwesomeAssertions;
using CloudNative.CloudEvents;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.CloudEvents.Envelopes;

public class CloudEventBrokerMessageTest
{
    private static readonly CloudEventEnvelopeMapper Mapper = new();
    private static readonly Uri Source = new("/orders", UriKind.Relative);

    [Fact]
    public void StructuredJson_WhenCrossingOutboundAndInbound_PreservesSeparateEventAndBrokerContexts()
    {
        var envelope = new PersistedDomainEvent(42, "order", "order-42", "created", new { Total = 10 },
            customAttributes: new Dictionary<string, string> { ["correlationid"] = "event-correlation" });
        var schema = new Uri("https://example.test/order.json");
        var cloudEvent = Mapper.ToCloudEvent(envelope, "event-id", Source, "application/vnd.order+json", schema);
        var attributes = new Dictionary<string, string> { ["correlationid"] = "broker-correlation", ["source"] = "broker-producer" };

        var outgoing = Mapper.ToOutboundMessage(cloudEvent, "outgoing-id", "orders-topic", "group-1", "dedup-1", attributes);
        var inbound = Mapper.ToInboundMessage(Mapper.ToCloudEvent(outgoing), "incoming-id", "orders-queue", "receipt-1", "group-1", attributes);
        var restored = Mapper.ToEnvelope(Mapper.ToCloudEvent(inbound));
        var forwarded = Mapper.ToOutboundMessage(Mapper.ToCloudEvent(restored), "forward-id", "archive-topic",
            partitionKey: inbound.PartitionKey, attributes: inbound.Attributes);
        var forwardedEvent = Mapper.ToCloudEvent(forwarded);

        outgoing.Id.Should().Be("outgoing-id");
        outgoing.Destination.Should().Be("orders-topic");
        outgoing.PartitionKey.Should().Be("group-1");
        outgoing.DeduplicationId.Should().Be("dedup-1");
        inbound.Id.Should().Be("incoming-id");
        inbound.Source.Should().Be("orders-queue");
        inbound.BrokerReference.Should().Be("receipt-1");
        inbound.PartitionKey.Should().Be("group-1");
        forwarded.Id.Should().Be("forward-id");
        forwarded.Attributes.Should().BeEquivalentTo(attributes);
        forwardedEvent.Id.Should().Be("event-id");
        forwardedEvent.Source.Should().Be(Source);
        forwardedEvent.DataSchema.Should().Be(schema);
        forwardedEvent.DataContentType.Should().Be("application/vnd.order+json");
        forwardedEvent["correlationid"].Should().Be("event-correlation");
        restored.Should().BeOfType<PersistedDomainEvent>().Which.Id.Should().Be(42);
        JObject.Parse(forwarded.Message)["data"]!["Total"]!.Value<int>().Should().Be(10);
    }

    [Fact]
    public void ToEnvelope_WhenExtensionsAreTyped_PreservesCanonicalValuesButNotTypes()
    {
        var cloudEvent = Mapper.ToCloudEvent(new DomainEventEnvelope("order", "42", "created"), "event-1", Source);
        cloudEvent[CloudEventAttribute.CreateExtension("accepted", CloudEventAttributeType.Boolean)] = true;
        cloudEvent[CloudEventAttribute.CreateExtension("count", CloudEventAttributeType.Integer)] = 12;
        cloudEvent[CloudEventAttribute.CreateExtension("signature", CloudEventAttributeType.Binary)] = new byte[] { 1, 2 };

        var inbound = Mapper.ToInboundMessage(cloudEvent, "message-1", "orders-queue", "receipt-1");
        var received = Mapper.ToCloudEvent(inbound);
        var directEnvelope = Mapper.ToEnvelope(cloudEvent);
        var envelope = Mapper.ToEnvelope(received);
        var remapped = Mapper.ToCloudEvent(envelope);

        received["accepted"].Should().Be(true);
        received["count"].Should().Be(12);
        envelope.CustomAttributes.Should().Contain("accepted", "true").And.Contain("count", "12").And.Contain("signature", "AQI=");
        directEnvelope.CustomAttributes.Should().BeEquivalentTo(envelope.CustomAttributes);
        remapped["accepted"].Should().Be("true");
        remapped["count"].Should().Be("12");
        remapped["signature"].Should().Be("AQI=");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"specversion\":\"1.0\",\"id\":\"one\",\"id\":\"two\",\"type\":\"created\",\"source\":\"/orders\"}")]
    [InlineData("{\"specversion\":\"1.0\",\"id\":\"one\",\"type\":\"created\",\"source\":\"/orders\",\"data\":{},\"data_base64\":\"AQI=\"}")]
    public void ToCloudEvent_WhenBrokerBodyIsMalformedOrColliding_RejectsIt(string body)
    {
        var inbound = new InboundMessage("message-1", "receipt-1", body, "orders-queue");

        var act = () => Mapper.ToCloudEvent(inbound);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be("body");
    }

    [Fact]
    public void ToCloudEvent_WhenStructuredMetadataUsesWrongType_DoesNotTreatItAsString()
    {
        const string body = """
            {"specversion":"1.0","id":"one","type":"created","source":"/orders","subject":"42",
             "time":"2025-02-03T02:05:06Z","anarchyaggregatetype":123}
            """;
        var outbound = new OutboundMessage("message-1", "orders-topic", body);

        var act = () => Mapper.ToEnvelope(Mapper.ToCloudEvent(outbound));

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(CloudEventEnvelopeMapper.AggregateTypeAttribute);
    }

    [Fact]
    public void CloudEvent_WhenExtensionIsNamedData_SdkRejectsCollision()
    {
        var cloudEvent = Mapper.ToCloudEvent(new DomainEventEnvelope("order", "42", "created"), "event-1", Source);

        var act = () => cloudEvent["data"] = "not the payload";

        act.Should().Throw<ArgumentException>();
    }
}
