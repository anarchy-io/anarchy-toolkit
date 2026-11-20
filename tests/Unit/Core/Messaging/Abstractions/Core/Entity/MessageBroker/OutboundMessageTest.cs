using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using System.Text;

namespace Anarchy.Toolkit.Tests.Unit.Core.Messaging.Abstractions.Core.Entity.MessageBroker;

public class OutboundMessageTest : TestCaseBase
{
    [Fact]
    public void Constructor_WithStringMessage_AssignsAllProperties()
    {
        var attributes = new Dictionary<string, string> { ["Key"] = "Value" };

        var result = new OutboundMessage("id", "destination", "message", "partition", "dedup", attributes);

        result.Id.Should().Be("id");
        result.Destination.Should().Be("destination");
        result.Message.Should().Be("message");
        result.PartitionKey.Should().Be("partition");
        result.DeduplicationId.Should().Be("dedup");
        result.Attributes.Should().BeEquivalentTo(attributes);
    }

    [Fact]
    public void Constructor_WithMessageBuffer_AssignsPartitionKeyAndDeduplicationId()
    {
        var result = new OutboundMessage(
            "id",
            "destination",
            Encoding.UTF8.GetBytes("message"),
            "partition",
            "dedup");

        result.Message.Should().Be("message");
        result.PartitionKey.Should().Be("partition");
        result.DeduplicationId.Should().Be("dedup");
    }

    [Fact]
    public void Constructor_WithEmptyMessageBuffer_ThrowsArgumentException()
    {
        var act = () => new OutboundMessage("id", "destination", ReadOnlySpan<byte>.Empty, "partition", "dedup");

        act.Should().Throw<ArgumentException>().WithParameterName("messageBuffer");
    }
}
