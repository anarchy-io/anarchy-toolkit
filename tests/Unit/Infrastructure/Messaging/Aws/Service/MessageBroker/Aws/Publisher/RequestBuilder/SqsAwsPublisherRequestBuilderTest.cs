using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Helper;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

public class SqsAwsPublisherRequestBuilderTest : TestCaseBase
{
    private const string NotGoodBeforeTimestamp = "NotGoodBeforeTimestamp";

    private static readonly AwsMessagingOptions FixtureOptions =
        new(AwsRegion.EuropeIreland.ToDescription(), "123456789012");

    public SqsAwsPublisherRequestBuilder Sut() => new(FixtureOptions);

    [Fact]
    public void FromOutboundMessage_WhenStandardQueue_BuildsExpectedSendMessageRequest()
    {
        // Arrange
        const string destination = "test-queue";
        const string body = "{\"foo\":\"bar\"}";

        var fixtureOutbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: destination,
            message: body,
            partitionKey: null,
            deduplicationId: TestHelper.RandomId(),
            attributes: new Dictionary<string, string> { ["Foo"] = "Bar" });

        var expectedQueueUrl =
            $"https://sqs.{FixtureOptions.Region}.amazonaws.com/{FixtureOptions.AccountId}/{destination}";

        // Act
        var result = Sut().FromOutboundMessage(fixtureOutbound);

        // Assert
        result.QueueUrl.Should().Be(expectedQueueUrl);
        result.MessageBody.Should().Be(body);
        result.MessageGroupId.Should().BeNull();
        result.MessageDeduplicationId.Should().BeNull();

        result.MessageAttributes.Should().ContainKey("Foo");
        result.MessageAttributes["Foo"].DataType.Should().Be("String");
        result.MessageAttributes["Foo"].StringValue.Should().Be("Bar");
    }

    [Fact]
    public void FromOutboundMessage_WhenFifoQueue_SetsFifoProperties()
    {
        // Arrange
        const string destination = "test-queue.fifo";
        const string body = "{\"foo\":\"bar\"}";
        const string partitionKey = "partition-key";
        var deduplicationId = TestHelper.RandomId();

        var fixtureOutbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: destination,
            message: body,
            partitionKey: partitionKey,
            deduplicationId: deduplicationId,
            attributes: new Dictionary<string, string>());

        // Act
        var result = Sut().FromOutboundMessage(fixtureOutbound);

        // Assert
        result.MessageGroupId.Should().Be(partitionKey);
        result.MessageDeduplicationId.Should().Be(deduplicationId);
    }

    [Fact]
    public void FromOutboundMessage_WhenMessageTooLargeAndCompressionDisabled_Throws()
    {
        // Arrange
        var largeBody = new string('a', 300_000); // > 256 KB
        var fixtureOutbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: "large-queue",
            message: largeBody,
            partitionKey: null,
            deduplicationId: TestHelper.RandomId(),
            attributes: new Dictionary<string, string>());

        var sut = new SqsAwsPublisherRequestBuilder(FixtureOptions, CompressionType.Uncompressed);

        // Act
        var act = () => sut.FromOutboundMessage(fixtureOutbound);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Message body is too large to be sent without compression*");
    }

    [Fact]
    public void FromOutboundMessage_WhenMessageTooLargeWithCompression_AddsCompressionAttributes()
    {
        // Arrange
        var largeBody = new string('a', 300_000); // > 256 KB
        var fixtureOutbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: "large-queue",
            message: largeBody,
            partitionKey: null,
            deduplicationId: TestHelper.RandomId(),
            attributes: new Dictionary<string, string> { ["Existing"] = "Attr" });

        var sut = new SqsAwsPublisherRequestBuilder(FixtureOptions, CompressionType.Gzip);

        // Act
        var result = sut.FromOutboundMessage(fixtureOutbound);

        // Assert
        result.MessageBody.Should().BeEmpty("large messages are moved into CompressionData");
        result.MessageAttributes.Should().ContainKey("Existing");
        result.MessageAttributes.Should().ContainKey("CompressionType");
        result.MessageAttributes.Should().ContainKey("CompressionData");

        var compressionTypeAttr = result.MessageAttributes["CompressionType"];
        compressionTypeAttr.DataType.Should().Be("String");
        compressionTypeAttr.StringValue.Should().Be(CompressionType.Gzip.ToDescription());

        var compressionDataAttr = result.MessageAttributes["CompressionData"];
        compressionDataAttr.DataType.Should().Be("Binary");
        compressionDataAttr.BinaryValue.Should().NotBeNull();
        compressionDataAttr.BinaryValue.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FromOutboundMessage_WhenNotGoodBeforeTimestampIsInNearFuture_SetsPositiveDelaySeconds()
    {
        // Arrange
        var target = DateHelper.Now().AddSeconds(30);
        var fixtureOutbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: "test-queue",
            message: "{\"foo\":\"bar\"}",
            partitionKey: null,
            deduplicationId: TestHelper.RandomId(),
            attributes: new Dictionary<string, string> {
                [NotGoodBeforeTimestamp] = target.ToString("O")
            });

        // Act
        var result = Sut().FromOutboundMessage(fixtureOutbound);

        // Assert
        // Allow a bit of drift between constructing the timestamp and calling DateHelper.Now()
        result.DelaySeconds.Should().BeGreaterThan(0);
        result.DelaySeconds.Should().BeLessThanOrEqualTo(30);
    }

    [Fact]
    public void FromOutboundMessage_WhenBatchInput_BuildsSendMessageBatchRequest()
    {
        // Arrange
        const string destination = "test-queue";
        var messages = new[] {
            new OutboundMessageEntity(
                id: TestHelper.RandomId(),
                destination: destination,
                message: "{\"foo\":\"bar\"}",
                partitionKey: null,
                deduplicationId: TestHelper.RandomId(),
                attributes: new Dictionary<string, string>()),
            new OutboundMessageEntity(
                id: TestHelper.RandomId(),
                destination: destination,
                message: "{\"foo\":\"baz\"}",
                partitionKey: null,
                deduplicationId: TestHelper.RandomId(),
                attributes: new Dictionary<string, string>())
        };

        // Act
        var result = Sut().FromOutboundMessage(messages);

        // Assert
        result.QueueUrl.Should().Contain(destination);
        result.Entries.Should().HaveCount(2);
    }
}
