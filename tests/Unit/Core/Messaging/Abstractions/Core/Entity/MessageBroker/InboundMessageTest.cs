using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Messaging.Abstractions.Core.Entity.MessageBroker;

public class InboundMessageTest : TestCaseBase
{
    [Fact]
    public void Constructor_WhenBrokerReferenceIsOpaque_AssignsItWithoutAssumingAcknowledgementSemantics()
    {
        var result = new InboundMessage("id", "topic:2:19", "body", "orders", partitionKey: "customer-1");

        result.BrokerReference.Should().Be("topic:2:19");
        result.Source.Should().Be("orders");
        result.PartitionKey.Should().Be("customer-1");
    }

    [Fact]
    public void Constructor_WithAttributes_StoresSnapshot()
    {
        var attributes = new Dictionary<string, string> { ["Key"] = "Value" };
        var result = new InboundMessage("id", "reference", "body", "orders", attributes: attributes);

        attributes.Clear();

        result.Attributes.Should().Contain("Key", "Value");
    }
}
