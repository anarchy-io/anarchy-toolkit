using Amazon.Lambda.SQSEvents;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Aws.Fixture;
using Anarchy.Toolkit.Testing.Helper;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Helper.MessageBroker;

public class InboundMessageMapperTest : TestCaseBase
{
    [Fact]
    public void FromSqs_WhenBodyIsUncompressed_ReturnsValidInboundMessage()
    {
        var groupId = TestHelper.RandomId("GroupId");
        var fixtureBody = TestHelper.RandomObject();
        var fixtureSqsMessage = SqsMessageFixture.WithUncompressedBody(fixtureBody, groupId);
        var result = InboundMessageMapper.FromSqs(fixtureSqsMessage);

        AssertResult(groupId, fixtureBody, fixtureSqsMessage, result);
    }

    [Fact]
    public void FromSqs_WhenBodyCompressedWithZlib_ReturnsValidInboundMessage()
    {
        var groupId = TestHelper.RandomId("GroupId");
        var fixtureBody = TestHelper.RandomObject();
        var fixtureSqsMessage = SqsMessageFixture.WithZlibBody(fixtureBody, groupId);
        var result = InboundMessageMapper.FromSqs(fixtureSqsMessage);

        AssertResult(groupId, fixtureBody, fixtureSqsMessage, result);
    }

    [Fact]
    public void FromSqs_WhenBodyCompressedWithGzip_ReturnsValidInboundMessage()
    {
        var groupId = TestHelper.RandomId("GroupId");
        var fixtureBody = TestHelper.RandomObject();
        var fixtureSqsMessage = SqsMessageFixture.WithGzipBody(fixtureBody, groupId);
        var result = InboundMessageMapper.FromSqs(fixtureSqsMessage);

        AssertResult(groupId, fixtureBody, fixtureSqsMessage, result);
    }

    [Fact]
    public void FromSqsEvent_WhenMixedCompressionStrategies_ReturnsValidInboundMessages()
    {
        var groupId = TestHelper.RandomId("GroupId");
        var fixtureBody = TestHelper.RandomObject();
        var fixtureSqsEvent = new SQSEvent {
            Records = [
                SqsMessageFixture.WithUncompressedBody(fixtureBody, groupId),
                SqsMessageFixture.WithZlibBody(fixtureBody, groupId),
                SqsMessageFixture.WithGzipBody(fixtureBody, groupId)
            ]
        };
        var result = InboundMessageMapper.FromSqs(fixtureSqsEvent);

        result.Length.Should().Be(fixtureSqsEvent.Records.Count);

        for (var idx = 0; idx < fixtureSqsEvent.Records.Count; idx++)
        {
            AssertResult(groupId, fixtureBody, fixtureSqsEvent.Records[idx], result[idx]);
        }
    }

    private static void AssertResult(string groupId, object? fixtureBody, SQSEvent.SQSMessage fixtureSqsMessage,
                                     InboundMessageEntity result)
    {
        using (new AssertionScope())
        {
            result.Id.Should().Be(fixtureSqsMessage.MessageId);
            result.ReceiptHandle.Should().Be(fixtureSqsMessage.ReceiptHandle);
            result.Source.Should().Be(fixtureSqsMessage.EventSourceArn);
            result.PartitionKey.Should().Be(groupId);
            result.Attributes.Keys.Should().Contain(["EmptyStringValue", "StringValue"]);
            result.Attributes["EmptyStringValue"].Should().Be(string.Empty);
            result.Attributes["StringValue"]
                .Should()
                .Be(fixtureSqsMessage.MessageAttributes["StringValue"].StringValue);

            JToken.Parse(result.Message)
                .Should()
                .BeEquivalentTo(JToken.Parse(fixtureBody?.ToJson() ?? "null"));
        }
    }
}
