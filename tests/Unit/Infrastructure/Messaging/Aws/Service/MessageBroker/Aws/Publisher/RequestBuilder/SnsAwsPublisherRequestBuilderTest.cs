using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Helper;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

public class SnsAwsPublisherRequestBuilderTest : TestCaseBase
{
    private static readonly AwsMessagingOptions FixtureOptions =
        new(AwsRegion.EuropeIreland.ToDescription(), "123456789012");

    public SnsAwsPublisherRequestBuilder Sut() => new(FixtureOptions);

    [Fact]
    public void FromOutboundMessage_WhenStandardQueue_BuildsExpectedSendMessageRequest()
    {
        // Arrange
        const string destination = "test-topic";
        const string body = "{\"foo\":\"bar\"}";

        var outbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: destination,
            message: body,
            partitionKey: null,
            deduplicationId: TestHelper.RandomId(),
            attributes: new Dictionary<string, string> {
                ["Foo"] = "Bar"
            });

        var expectedTopicArn = $"arn:aws:sns:{FixtureOptions.Region}:{FixtureOptions.AccountId}:{destination}";

        // Act
        var result = Sut().FromOutboundMessage(outbound);

        // Assert
        result.TopicArn.Should().Be(expectedTopicArn);
        result.Message.Should().Be(body);
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
        const string destination = "test-topic.fifo";
        const string body = "{\"foo\":\"bar\"}";
        const string partitionKey = "partition-key";
        var deduplicationId = TestHelper.RandomId();

        var outbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: destination,
            message: body,
            partitionKey: partitionKey,
            deduplicationId: deduplicationId,
            attributes: new Dictionary<string, string>());

        // Act
        var result = Sut().FromOutboundMessage(outbound);

        // Assert
        result.MessageGroupId.Should().Be(partitionKey);
        result.MessageDeduplicationId.Should().Be(deduplicationId);
    }

    [Fact]
    public void FromOutboundMessage_WhenMessageTooLargeAndCompressionDisabled_Throws()
    {
        // Arrange
        var largeBody = new string('a', 300_000); // > 256 KB
        var outbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: "large-queue",
            message: largeBody,
            partitionKey: null,
            deduplicationId: TestHelper.RandomId(),
            attributes: new Dictionary<string, string>());

        var sut = new SnsAwsPublisherRequestBuilder(FixtureOptions, CompressionType.Uncompressed);

        // Act
        var act = () => sut.FromOutboundMessage(outbound);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Message body is too large to be sent without compression*");
    }

    [Fact]
    public void FromOutboundMessage_WhenMessageTooLargeWithCompression_AddsCompressionAttributes()
    {
        // Arrange
        var largeBody = new string('a', 300_000); // > 256 KB
        var outbound = new OutboundMessageEntity(
            id: TestHelper.RandomId(),
            destination: "large-queue",
            message: largeBody,
            partitionKey: null,
            deduplicationId: TestHelper.RandomId(),
            attributes: new Dictionary<string, string> {
                ["Existing"] = "Attr"
            });

        var sut = new SnsAwsPublisherRequestBuilder(FixtureOptions, CompressionType.Gzip);

        // Act
        var result = sut.FromOutboundMessage(outbound);

        // Assert
        result.Message.Should().BeEmpty("large messages are moved into CompressionData");
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
    public void FromOutboundMessage_WhenBatchInput_BuildsPublishBatchRequest()
    {
        // Arrange
        const string destination = "test-topic";
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
        result.TopicArn.Should().Contain(destination);
        result.PublishBatchRequestEntries.Should().HaveCount(2);
    }
}
