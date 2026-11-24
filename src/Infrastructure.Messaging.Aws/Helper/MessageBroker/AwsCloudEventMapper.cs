using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Mime;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker;

/// <summary>Maps structured JSON CloudEvents to AWS publish requests and received AWS messages.</summary>
/// <remarks>
/// CloudEvent identity and source remain in the body; broker identity, source, destination and string attributes are
/// separate. AWS assigns received message IDs, so request-side message IDs do not become received IDs. Only string
/// broker attributes are represented by the toolkit messages. Binary-mode CloudEvents are not supported.
/// </remarks>
public static class AwsCloudEventMapper
{
    /// <summary>Broker string attribute used to indicate the structured event body's media type.</summary>
    public const string ContentTypeAttribute = "ContentType";

    /// <summary>Builds an SQS send request, replacing only the outbound message body with the structured event.</summary>
    /// <param name="cloudEvent">Event to serialize.</param>
    /// <param name="message">Outbound destination, identity, FIFO fields and message attributes.</param>
    /// <param name="builder">The configured SQS request builder.</param>
    /// <returns>A request ready to send to SQS.</returns>
    /// <exception cref="AwsCloudEventEmptyBodyException">The builder produced an empty body, for example after compression.</exception>
    public static SendMessageRequest ToSqsRequest(CloudEvent cloudEvent, OutboundMessage message,
                                                   SqsAwsPublisherRequestBuilder builder)
    {
        if (builder is null) throw new EmptyArgumentException(nameof(builder));
        var request = builder.FromOutboundMessage(WithEventBody(cloudEvent, message));
        if (string.IsNullOrEmpty(request.MessageBody)) throw new AwsCloudEventEmptyBodyException("SQS");
        return request;
    }

    /// <summary>Builds an SNS publish request, replacing only the outbound message body with the structured event.</summary>
    /// <param name="cloudEvent">Event to serialize.</param>
    /// <param name="message">Outbound destination, identity, FIFO fields and message attributes.</param>
    /// <param name="builder">The configured SNS request builder.</param>
    /// <returns>A request ready to publish to SNS.</returns>
    /// <exception cref="AwsCloudEventEmptyBodyException">The builder produced an empty body, for example after compression.</exception>
    public static PublishRequest ToSnsRequest(CloudEvent cloudEvent, OutboundMessage message,
                                              SnsAwsPublisherRequestBuilder builder)
    {
        if (builder is null) throw new EmptyArgumentException(nameof(builder));
        var request = builder.FromOutboundMessage(WithEventBody(cloudEvent, message));
        if (string.IsNullOrEmpty(request.Message)) throw new AwsCloudEventEmptyBodyException("SNS");
        return request;
    }

    /// <summary>Reads structured events from all records in a Lambda SQS event.</summary>
    /// <param name="sqsEvent">The received Lambda event.</param>
    /// <param name="extensions">Known typed CloudEvent extension attributes, if any.</param>
    /// <returns>Events and their received broker messages in record order.</returns>
    public static IReadOnlyList<AwsReceivedCloudEvent> FromSqs(SQSEvent sqsEvent,
                                                                IEnumerable<CloudEventAttribute>? extensions = null)
    {
        if (sqsEvent is null) throw new EmptyArgumentException(nameof(sqsEvent));
        var knownExtensions = extensions?.ToArray();
        return sqsEvent.Records.Select(record => FromSqs(record, knownExtensions)).ToArray();
    }

    /// <summary>Reads a structured event and broker metadata from a Lambda SQS record.</summary>
    /// <param name="message">Received record.</param>
    /// <param name="extensions">Known typed CloudEvent extension attributes, if any.</param>
    /// <returns>The event and its received broker message.</returns>
    public static AwsReceivedCloudEvent FromSqs(SQSEvent.SQSMessage message,
                                                 IEnumerable<CloudEventAttribute>? extensions = null)
    {
        if (message is null) throw new EmptyArgumentException(nameof(message));
        var inbound = InboundMessageMapper.FromSqs(message);
        return new AwsReceivedCloudEvent(Decode(StructuredBody(inbound.Message, message.MessageAttributes), extensions), inbound);
    }

    /// <summary>Reads a structured event from an AWS SDK SQS receive response message.</summary>
    /// <param name="message">Received message (request the needed message and system attributes when receiving).</param>
    /// <param name="queueSource">Queue ARN or URL; SDK receive messages do not contain the source.</param>
    /// <param name="extensions">Known typed CloudEvent extension attributes, if any.</param>
    /// <returns>The event and its received broker message.</returns>
    public static AwsReceivedCloudEvent FromSqs(Message message, string queueSource,
                                                 IEnumerable<CloudEventAttribute>? extensions = null)
    {
        if (message is null) throw new EmptyArgumentException(nameof(message));
        if (string.IsNullOrWhiteSpace(queueSource)) throw new EmptyArgumentException(nameof(queueSource));
        return FromSqs(ToLambdaMessage(message, queueSource), extensions);
    }

    /// <summary>Reads an SNS notification JSON document, such as a non-raw SNS-to-SQS delivery body.</summary>
    /// <param name="notificationJson">SNS Notification JSON, not an SNS publish request.</param>
    /// <param name="extensions">Known typed CloudEvent extension attributes, if any.</param>
    /// <returns>The event and SNS notification metadata.</returns>
    public static AwsSnsCloudEvent FromSnsNotification(string notificationJson,
                                                        IEnumerable<CloudEventAttribute>? extensions = null)
    {
        if (string.IsNullOrWhiteSpace(notificationJson)) throw new EmptyArgumentException(nameof(notificationJson));
        JObject notification;
        try { notification = JObject.Parse(notificationJson); }
        catch (JsonException ex) { throw new AwsCloudEventJsonParsingException("SNS notification", ex); }
        var id = StringProperty(notification, "MessageId");
        var topic = StringProperty(notification, "TopicArn");
        var body = StringProperty(notification, "Message");
        if (StringProperty(notification, "Type") != "Notification" || string.IsNullOrWhiteSpace(id) ||
            string.IsNullOrWhiteSpace(topic) || body is null)
            throw new InvalidFormatException(notificationJson, typeof(AwsSnsCloudEvent));

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        var messageAttributes = notification["MessageAttributes"] as JObject;
        if (messageAttributes is not null)
        {
            foreach (var attribute in messageAttributes.Properties())
            {
                if (attribute.Value is JObject attributeObject && StringProperty(attributeObject, "Type") == "String" &&
                    StringProperty(attributeObject, "Value") is string value)
                    attributes[attribute.Name] = value;
            }
        }

        if (messageAttributes?["CompressionData"] is JObject compressionData &&
            StringProperty(compressionData, "Type") == "Binary" &&
            StringProperty(compressionData, "Value") is string encoded &&
            attributes.TryGetValue("CompressionType", out var compressionType) &&
            (string.Equals(compressionType, "gzip", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(compressionType, "zlib", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = new MemoryStream(Convert.FromBase64String(encoded));
            var decompressed = CompressionHelper.DecompressToUtf8(stream,
                string.Equals(compressionType, "gzip", StringComparison.OrdinalIgnoreCase)
                    ? CompressionType.Gzip : CompressionType.Zlib);
            body = UnwrapCompressedString(decompressed);
        }

        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidFormatException(notificationJson, typeof(AwsSnsCloudEvent));

        return new AwsSnsCloudEvent(Decode(body, extensions), id, topic, body, attributes);
    }

    /// <summary>Reads a non-raw SNS notification delivered inside a Lambda SQS record.</summary>
    /// <param name="message">Received SQS record.</param>
    /// <param name="extensions">Known typed CloudEvent extension attributes, if any.</param>
    /// <returns>Both SQS and SNS metadata, kept separate to avoid attribute collisions.</returns>
    public static AwsSnsSqsCloudEvent FromSnsSqs(SQSEvent.SQSMessage message,
                                                  IEnumerable<CloudEventAttribute>? extensions = null)
    {
        if (message is null) throw new EmptyArgumentException(nameof(message));
        var inbound = InboundMessageMapper.FromSqs(message);
        return new AwsSnsSqsCloudEvent(FromSnsNotification(StructuredBody(inbound.Message, message.MessageAttributes), extensions), inbound);
    }

    /// <summary>Reads a non-raw SNS notification delivered inside an SDK SQS receive message.</summary>
    /// <param name="message">Received SQS message.</param>
    /// <param name="queueSource">Queue ARN or URL; SDK receive messages do not contain the source.</param>
    /// <param name="extensions">Known typed CloudEvent extension attributes, if any.</param>
    /// <returns>Both SQS and SNS metadata, kept separate to avoid attribute collisions.</returns>
    public static AwsSnsSqsCloudEvent FromSnsSqs(Message message, string queueSource,
                                                  IEnumerable<CloudEventAttribute>? extensions = null)
    {
        if (message is null) throw new EmptyArgumentException(nameof(message));
        if (string.IsNullOrWhiteSpace(queueSource)) throw new EmptyArgumentException(nameof(queueSource));
        return FromSnsSqs(ToLambdaMessage(message, queueSource), extensions);
    }

    private static OutboundMessage WithEventBody(CloudEvent cloudEvent, OutboundMessage message)
    {
        if (cloudEvent is null) throw new EmptyArgumentException(nameof(cloudEvent));
        if (message is null) throw new EmptyArgumentException(nameof(message));
        var bytes = new JsonEventFormatter().EncodeStructuredModeMessage(cloudEvent, out var contentType);
        var attributes = new Dictionary<string, string>(message.Attributes, StringComparer.Ordinal) {
            [ContentTypeAttribute] = contentType.ToString()
        };
        return new OutboundMessage(message.Id, message.Destination, Encoding.UTF8.GetString(bytes.Span),
            message.PartitionKey, message.DeduplicationId, attributes);
    }

    private static CloudEvent Decode(string body, IEnumerable<CloudEventAttribute>? extensions)
    {
        try
        {
            return new JsonEventFormatter().DecodeStructuredModeMessage(Encoding.UTF8.GetBytes(body),
                new ContentType("application/cloudevents+json"), extensions);
        }
        catch (JsonException ex) { throw new AwsCloudEventJsonParsingException("CloudEvent", ex); }
    }

    private static string? StringProperty(JObject value, string name) =>
        value[name] is JValue { Type: JTokenType.String } property ? property.Value<string>() : null;

    private static SQSEvent.SQSMessage ToLambdaMessage(Message message, string source) => new() {
        MessageId = message.MessageId,
        ReceiptHandle = message.ReceiptHandle,
        Body = message.Body,
        EventSourceArn = source,
        Attributes = message.Attributes,
        MessageAttributes = message.MessageAttributes?.ToDictionary(pair => pair.Key, pair => new SQSEvent.MessageAttribute {
            DataType = pair.Value.DataType,
            StringValue = pair.Value.StringValue,
            BinaryValue = pair.Value.BinaryValue
        }, StringComparer.Ordinal)
    };

    // The existing publisher compresses a JSON-serialized string rather than the string itself.
    private static string StructuredBody(string body, Dictionary<string, SQSEvent.MessageAttribute>? attributes)
    {
        if (attributes?.ContainsKey("CompressionData") == true &&
            attributes.TryGetValue("CompressionType", out var type) &&
            (string.Equals(type.StringValue, "Gzip", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(type.StringValue, "Zlib", StringComparison.OrdinalIgnoreCase)))
        {
            return UnwrapCompressedString(body);
        }

        return body;
    }

    private static string UnwrapCompressedString(string body)
    {
        try
        {
            var token = JToken.Parse(body);
            return token.Type == JTokenType.String ? token.Value<string>()! : body;
        }
        catch (JsonException ex) { throw new AwsCloudEventJsonParsingException("compressed body", ex); }
    }
}
