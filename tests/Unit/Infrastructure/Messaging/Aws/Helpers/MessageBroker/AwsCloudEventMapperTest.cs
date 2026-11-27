using Amazon.Lambda.SQSEvents;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.Compression;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using CloudNative.CloudEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Mime;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Helpers.MessageBroker;

public class AwsCloudEventMapperTest : TestCaseBase
{
    private static readonly AwsMessagingOptions Options = new("eu-west-1", "123456789012");

    [Fact]
    public void Sqs_WhenSentAndReceivedAsLambdaRecord_PreservesEventAndBrokerMetadata()
    {
        var cloudEvent = Event();
        var metadata = Metadata("orders.fifo");
        var request = AwsCloudEventMapper.ToSqsRequest(cloudEvent, metadata, new SqsAwsPublisherRequestBuilder(Options));
        var record = LambdaRecord(request);

        var result = AwsCloudEventMapper.FromSqs(record);

        request.QueueUrl.Should().Be("https://sqs.eu-west-1.amazonaws.com/123456789012/orders.fifo");
        request.MessageGroupId.Should().Be("order-42");
        request.MessageDeduplicationId.Should().Be("dedup-1");
        result.Message.Id.Should().Be("sqs-assigned-id");
        result.Message.BrokerReference.Should().Be("receipt");
        result.Message.Source.Should().Be("arn:aws:sqs:eu-west-1:123456789012:orders.fifo");
        result.Message.PartitionKey.Should().Be("order-42");
        foreach (var attribute in metadata.Attributes)
            result.Message.Attributes[attribute.Key].Should().Be(attribute.Value);
        request.MessageAttributes[AwsCloudEventMapper.ContentTypeAttribute].DataType.Should().Be("String");
        new ContentType(result.Message.Attributes[AwsCloudEventMapper.ContentTypeAttribute]).MediaType
            .Should().Be("application/cloudevents+json");
        result.Message.Message.Should().Be(request.MessageBody);
        AssertEvent(result.CloudEvent);
    }

    [Fact]
    public void Sqs_WhenReceivedAsSdkMessage_PreservesCallerSuppliedSourceAndAttributes()
    {
        var request = AwsCloudEventMapper.ToSqsRequest(Event(), Metadata("orders.fifo"),
            new SqsAwsPublisherRequestBuilder(Options));
        var received = new Message {
            MessageId = "sdk-id",
            ReceiptHandle = "sdk-receipt",
            Body = request.MessageBody,
            Attributes = new Dictionary<string, string> { ["MessageGroupId"] = request.MessageGroupId },
            MessageAttributes = request.MessageAttributes
        };

        var result = AwsCloudEventMapper.FromSqs(received, "queue-source");

        result.Message.Source.Should().Be("queue-source");
        result.Message.Id.Should().Be("sdk-id");
        result.Message.PartitionKey.Should().Be("order-42");
        result.Message.Attributes["CorrelationId"].Should().Be("correlation-1");
        AssertEvent(result.CloudEvent);
    }

    [Fact]
    public void Sqs_WhenRepublishing_LeavesCorrelationAndEventAttributesAndUpdatesBrokerSource()
    {
        var firstRequest = AwsCloudEventMapper.ToSqsRequest(Event(), Metadata("orders.fifo"),
            new SqsAwsPublisherRequestBuilder(Options));
        var received = AwsCloudEventMapper.FromSqs(LambdaRecord(firstRequest));
        var outbound = new OutboundMessageBuilder("retry.fifo", "retry-service").Build(
            received.Message.Message, received.Message.PartitionKey, received.Message.Attributes);

        var republished = AwsCloudEventMapper.ToSqsRequest(received.CloudEvent, outbound,
            new SqsAwsPublisherRequestBuilder(Options));

        republished.MessageGroupId.Should().Be("order-42");
        republished.MessageDeduplicationId.Should().Be(outbound.DeduplicationId);
        republished.MessageAttributes["Source"].StringValue.Should().Be("retry-service");
        republished.MessageAttributes["TypeAggregate"].StringValue.Should().Be("order");
        republished.MessageAttributes["AggregateId"].StringValue.Should().Be("order-42");
        republished.MessageAttributes["TypeEvent"].StringValue.Should().Be("order-created");
        republished.MessageAttributes["CorrelationId"].StringValue.Should().Be("correlation-1");
        received.CloudEvent.Source!.OriginalString.Should().Be("/ordering");
        received.Message.Attributes["Source"].Should().Be("ordering");
    }

    [Fact]
    public void Sns_WhenDeliveredAsNotificationInsideSqs_PreservesSeparateMetadata()
    {
        var metadata = Metadata("orders.fifo");
        var request = AwsCloudEventMapper.ToSnsRequest(Event(), metadata, new SnsAwsPublisherRequestBuilder(Options));
        var notification = new JObject {
            ["Type"] = "Notification",
            ["MessageId"] = "sns-assigned-id",
            ["TopicArn"] = request.TopicArn,
            ["Message"] = request.Message,
            ["MessageAttributes"] = new JObject(request.MessageAttributes.Select(pair =>
                new JProperty(pair.Key, new JObject { ["Type"] = pair.Value.DataType, ["Value"] = pair.Value.StringValue })))
        };
        var record = new SQSEvent.SQSMessage {
            MessageId = "sqs-assigned-id",
            ReceiptHandle = "receipt",
            EventSourceArn = "queue-arn",
            Body = notification.ToString(),
            Attributes = new Dictionary<string, string> { ["MessageGroupId"] = "order-42" },
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute> {
                ["Source"] = new() { DataType = "String", StringValue = "queue-source" }
            }
        };

        var result = AwsCloudEventMapper.FromSnsSqs(record);

        request.TopicArn.Should().Be("arn:aws:sns:eu-west-1:123456789012:orders.fifo");
        request.MessageGroupId.Should().Be("order-42");
        request.MessageDeduplicationId.Should().Be("dedup-1");
        result.Notification.MessageId.Should().Be("sns-assigned-id");
        result.Notification.TopicArn.Should().Be(request.TopicArn);
        result.Notification.Message.Should().Be(request.Message);
        foreach (var attribute in metadata.Attributes)
            result.Notification.Attributes[attribute.Key].Should().Be(attribute.Value);
        request.MessageAttributes[AwsCloudEventMapper.ContentTypeAttribute].DataType.Should().Be("String");
        new ContentType(result.Notification.Attributes[AwsCloudEventMapper.ContentTypeAttribute]).MediaType
            .Should().Be("application/cloudevents+json");
        result.Message.Id.Should().Be("sqs-assigned-id");
        result.Message.Source.Should().Be("queue-arn");
        result.Message.Attributes["Source"].Should().Be("queue-source");
        result.Message.PartitionKey.Should().Be("order-42");
        AssertEvent(result.Notification.CloudEvent);
        AwsCloudEventMapper.FromSnsNotification(notification.ToString()).CloudEvent.Id.Should().Be("event-123");
    }

    [Fact]
    public void Requests_WhenBuilderCompressesAndProducesEmptyBody_RejectBothTransports()
    {
        var cloudEvent = Event();
        cloudEvent.Data = new JObject { ["description"] = new string('a', 300_000) };
        var metadata = Metadata("orders.fifo");
        var sqs = () => AwsCloudEventMapper.ToSqsRequest(cloudEvent, metadata, new SqsAwsPublisherRequestBuilder(Options));
        var sns = () => AwsCloudEventMapper.ToSnsRequest(cloudEvent, metadata, new SnsAwsPublisherRequestBuilder(Options));

        sqs.Should().Throw<AwsCloudEventEmptyBodyException>().Which.Transport.Should().Be("SQS");
        sns.Should().Throw<AwsCloudEventEmptyBodyException>().Which.Transport.Should().Be("SNS");
    }

    [Fact]
    public void Sqs_WhenReceivedCompressed_DecodesEventAndPreservesMetadata()
    {
        var request = AwsCloudEventMapper.ToSqsRequest(Event(), Metadata("orders.fifo"),
            new SqsAwsPublisherRequestBuilder(Options));
        var received = LambdaRecord(request);
        received.Body = "compressed";
        received.MessageAttributes["CompressionType"] = new SQSEvent.MessageAttribute { DataType = "String", StringValue = "gzip" };
        received.MessageAttributes["CompressionData"] = new SQSEvent.MessageAttribute {
            DataType = "Binary",
            BinaryValue = new MemoryStream(CompressionHelper.CompressFromUtf8(JsonConvert.SerializeObject(request.MessageBody), CompressionType.Gzip))
        };

        var result = AwsCloudEventMapper.FromSqs(received);

        result.Message.Attributes["CorrelationId"].Should().Be("correlation-1");
        AssertEvent(result.CloudEvent);
    }

    [Fact]
    public void Sns_WhenCompressedNotificationArrivesViaSdkSqs_DecodesEventAndMetadata()
    {
        var request = AwsCloudEventMapper.ToSnsRequest(Event(), Metadata("orders.fifo"),
            new SnsAwsPublisherRequestBuilder(Options));
        var compressed = CompressionHelper.CompressFromUtf8(JsonConvert.SerializeObject(request.Message), CompressionType.Gzip);
        var notification = new JObject {
            ["Type"] = "Notification",
            ["MessageId"] = "sns-id",
            ["TopicArn"] = request.TopicArn,
            ["Message"] = "compressed",
            ["MessageAttributes"] = new JObject {
                ["CompressionType"] = new JObject { ["Type"] = "String", ["Value"] = "gzip" },
                ["CompressionData"] = new JObject { ["Type"] = "Binary", ["Value"] = Convert.ToBase64String(compressed) },
                ["CorrelationId"] = new JObject { ["Type"] = "String", ["Value"] = "correlation-1" }
            }
        };
        var received = new Message {
            MessageId = "sqs-id",
            ReceiptHandle = "receipt",
            Body = notification.ToString(),
            Attributes = new Dictionary<string, string> { ["MessageGroupId"] = "order-42" }
        };

        var result = AwsCloudEventMapper.FromSnsSqs(received, "queue-source");

        result.Message.Source.Should().Be("queue-source");
        result.Message.Id.Should().Be("sqs-id");
        result.Notification.MessageId.Should().Be("sns-id");
        result.Notification.Attributes["CorrelationId"].Should().Be("correlation-1");
        AssertEvent(result.Notification.CloudEvent);
    }

    [Fact]
    public void Requests_WhenContentTypeCollides_OverwriteReservedKeyWithoutMutatingMetadata()
    {
        var attributes = new Dictionary<string, string> {
            ["ContentType"] = "text/plain",
            ["contenttype"] = "independent",
            ["CorrelationId"] = "correlation-1"
        };
        var metadata = new OutboundMessage("outbound-1", "orders.fifo", "placeholder", "group", "dedup", attributes);
        var sqs = AwsCloudEventMapper.ToSqsRequest(Event(), metadata, new SqsAwsPublisherRequestBuilder(Options));
        var sns = AwsCloudEventMapper.ToSnsRequest(Event(), metadata, new SnsAwsPublisherRequestBuilder(Options));

        new ContentType(sqs.MessageAttributes["ContentType"].StringValue).MediaType.Should().Be("application/cloudevents+json");
        new ContentType(sns.MessageAttributes["ContentType"].StringValue).MediaType.Should().Be("application/cloudevents+json");
        sqs.MessageAttributes["contenttype"].StringValue.Should().Be("independent");
        sns.MessageAttributes["CorrelationId"].StringValue.Should().Be("correlation-1");
        attributes["ContentType"].Should().Be("text/plain");
        JObject.Parse(sqs.MessageBody)["datacontenttype"]!.Value<string>().Should().Be("application/json");
        JObject.Parse(sns.Message)["datacontenttype"]!.Value<string>().Should().Be("application/json");
    }

    [Fact]
    public void FromSnsNotification_WhenJsonIsMalformed_ThrowsParsingExceptionWithCause()
    {
        var act = () => AwsCloudEventMapper.FromSnsNotification("{invalid");

        var error = act.Should().Throw<AwsCloudEventJsonParsingException>().Which;
        error.Representation.Should().Be("SNS notification");
        error.InnerException.Should().BeAssignableTo<JsonException>();
    }

    [Fact]
    public void FromSqs_WhenCloudEventJsonIsMalformed_ThrowsParsingExceptionWithCause()
    {
        var request = AwsCloudEventMapper.ToSqsRequest(Event(), Metadata("orders.fifo"), new SqsAwsPublisherRequestBuilder(Options));
        var received = LambdaRecord(request);
        received.Body = "{invalid";
        var act = () => AwsCloudEventMapper.FromSqs(received);

        var error = act.Should().Throw<AwsCloudEventJsonParsingException>().Which;
        error.Representation.Should().Be("CloudEvent");
        error.InnerException.Should().BeAssignableTo<JsonException>();
    }

    [Fact]
    public void Mapper_WhenRequiredArgumentIsNull_ThrowsEmptyArgumentException()
    {
        var nullEvent = () => AwsCloudEventMapper.ToSqsRequest(null!, Metadata("orders.fifo"), new SqsAwsPublisherRequestBuilder(Options));
        var nullBuilder = () => AwsCloudEventMapper.ToSnsRequest(Event(), Metadata("orders.fifo"), null!);
        var nullMessage = () => AwsCloudEventMapper.FromSqs((SQSEvent.SQSMessage)null!);
        var nullNotification = () => AwsCloudEventMapper.FromSnsNotification(null!);

        nullEvent.Should().Throw<EmptyArgumentException>();
        nullBuilder.Should().Throw<EmptyArgumentException>();
        nullMessage.Should().Throw<EmptyArgumentException>();
        nullNotification.Should().Throw<EmptyArgumentException>();
    }

    [Fact]
    public void FromSnsNotification_WhenNotNotification_RejectsDocument()
    {
        var act = () => AwsCloudEventMapper.FromSnsNotification("{\"Type\":\"SubscriptionConfirmation\"}");

        act.Should().Throw<Anarchy.Toolkit.Core.Exceptions.InvalidFormatException>();
    }

    private static CloudEvent Event() => new(CloudEventsSpecVersion.V1_0) {
        Id = "event-123",
        Source = new Uri("/ordering", UriKind.Relative),
        Type = "order-created",
        Subject = "order-42",
        Time = DateTimeOffset.Parse("2026-01-02T03:04:05Z"),
        DataContentType = "application/json",
        Data = new JObject { ["total"] = 10 },
        ["anarchyaggregatetype"] = "order"
    };

    private static OutboundMessage Metadata(string destination) => new("outbound-1", destination, "placeholder",
        "order-42", "dedup-1", new Dictionary<string, string> {
            ["Source"] = "ordering",
            ["TypeAggregate"] = "order",
            ["AggregateId"] = "order-42",
            ["TypeEvent"] = "order-created",
            ["CorrelationId"] = "correlation-1"
        });

    private static SQSEvent.SQSMessage LambdaRecord(SendMessageRequest request) => new() {
        MessageId = "sqs-assigned-id",
        ReceiptHandle = "receipt",
        EventSourceArn = "arn:aws:sqs:eu-west-1:123456789012:orders.fifo",
        Body = request.MessageBody,
        Attributes = new Dictionary<string, string> { ["MessageGroupId"] = request.MessageGroupId },
        MessageAttributes = request.MessageAttributes.ToDictionary(pair => pair.Key,
            pair => new SQSEvent.MessageAttribute {
                DataType = pair.Value.DataType,
                StringValue = pair.Value.StringValue,
                BinaryValue = pair.Value.BinaryValue
            })
    };

    private static void AssertEvent(CloudEvent cloudEvent)
    {
        cloudEvent.Id.Should().Be("event-123");
        cloudEvent.Source!.OriginalString.Should().Be("/ordering");
        cloudEvent.Type.Should().Be("order-created");
        cloudEvent.Subject.Should().Be("order-42");
        cloudEvent.Time.Should().Be(DateTimeOffset.Parse("2026-01-02T03:04:05Z"));
        cloudEvent["anarchyaggregatetype"].Should().Be("order");
        ((JObject)cloudEvent.Data!)["total"]!.Value<int>().Should().Be(10);
    }
}
