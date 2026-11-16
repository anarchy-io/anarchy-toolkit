using Amazon.Lambda.SQSEvents;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using Anarchy.Toolkit.Testing.Helper;

namespace Anarchy.Toolkit.Testing.Aws.Fixture;

/// <summary>
/// Fixture for building <see cref="SQSEvent.SQSMessage"/> instances with optional
/// compressed bodies (gzip/zlib) and random message attributes for testing.
/// </summary>
/// <remarks>
/// Compressed variants follow the convention read by the toolkit's AWS messaging infrastructure: the body is left
/// empty and the compressed JSON payload is carried in the binary <c>CompressionData</c> message attribute, with
/// the algorithm in the <c>CompressionType</c> attribute.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var sqsEvent = new SQSEvent { Records = [SqsMessageFixture.WithGzipBody(new { OrderId = 42 }, groupId: "orders")] };
/// await handler.FunctionHandler(sqsEvent, new LambdaContextFixture());
/// </code>
/// </example>
public class SqsMessageFixture
{
    /// <summary>The AWS region assigned to fixture messages.</summary>
    public const string DefaultAwsRegion = "eu-west-1";
    /// <summary>The event source assigned to fixture messages.</summary>
    public const string DefaultEventSource = "aws:sqs";
    /// <summary>The fake AWS account id used in <see cref="DefaultEventSourceArn"/>.</summary>
    public const string DefaultAccount = "123456789012";
    /// <summary>The fake FIFO queue name used in <see cref="DefaultEventSourceArn"/>.</summary>
    public const string DefaultQueueName = "fixture-queue.fifo";

    /// <summary>The event source ARN assigned to fixture messages.</summary>
    public const string DefaultEventSourceArn =
        $"arn:{DefaultEventSource}:{DefaultAwsRegion}:{DefaultAccount}:{DefaultQueueName}";

    /// <summary>
    /// Builds a message without body, with random message and receipt ids and the default source, ARN and region.
    /// </summary>
    /// <param name="groupId">Optional FIFO <c>MessageGroupId</c> attribute; when <see langword="null"/>, <see cref="SQSEvent.SQSMessage.Attributes"/> is <see langword="null"/>.</param>
    /// <param name="withRandomAttributes">Whether to add sample message attributes (random, empty, null string and binary values).</param>
    /// <returns>A new message.</returns>
    public static SQSEvent.SQSMessage BaseMessage(string? groupId = null, bool withRandomAttributes = true)
    {
        return new SQSEvent.SQSMessage {
            MessageId = TestHelper.RandomId("Fixture"),
            ReceiptHandle = TestHelper.RandomId(),
            EventSourceArn = DefaultEventSourceArn,
            EventSource = DefaultEventSource,
            AwsRegion = DefaultAwsRegion,
            Attributes = groupId is null ? null : new() { { "MessageGroupId", groupId } },
            MessageAttributes = withRandomAttributes ? RandomMessageAttributes() : null
        };
    }

    /// <summary>
    /// Builds a message (see <see cref="BaseMessage"/>) whose body is <paramref name="payload"/> serialized as JSON,
    /// with the corresponding MD5 of body.
    /// </summary>
    /// <param name="payload">The payload to serialize, or <see langword="null"/> for no body.</param>
    /// <param name="groupId">Optional FIFO <c>MessageGroupId</c> attribute.</param>
    /// <param name="withRandomAttributes">Whether to add sample message attributes.</param>
    /// <returns>A new message.</returns>
    public static SQSEvent.SQSMessage WithUncompressedBody(object? payload = null, string? groupId = null,
                                                           bool withRandomAttributes = true)
    {
        var result = BaseMessage(groupId, withRandomAttributes);

        result.Body = payload?.ToJson();
        result.Md5OfBody = payload is null ? null : HashHelper.HashWithMd5(payload);

        return result;
    }

    /// <summary>
    /// Builds a message (see <see cref="BaseMessage"/>) carrying <paramref name="payload"/> as gzip-compressed JSON in the
    /// <c>CompressionData</c> attribute.
    /// </summary>
    /// <param name="payload">The payload to compress, or <see langword="null"/> to return the base message unchanged.</param>
    /// <param name="groupId">Optional FIFO <c>MessageGroupId</c> attribute.</param>
    /// <param name="withRandomAttributes">Whether to add sample message attributes.</param>
    /// <returns>A new message.</returns>
    public static SQSEvent.SQSMessage WithGzipBody(object? payload = null, string? groupId = null,
                                                   bool withRandomAttributes = true)
    {
        var result = BaseMessage(groupId, withRandomAttributes);

        if (payload is null) return result;

        result.MessageAttributes ??= new Dictionary<string, SQSEvent.MessageAttribute>();
        result.MessageAttributes["CompressionType"] = new SQSEvent.MessageAttribute {
            DataType = "String",
            StringValue = CompressionType.Gzip.ToDescription()
        };

        result.MessageAttributes["CompressionData"] = new SQSEvent.MessageAttribute {
            DataType = "Binary",
            BinaryValue = new MemoryStream(CompressionHelper.CompressFromUtf8(payload.ToJson(), CompressionType.Gzip))
        };

        return result;
    }

    /// <summary>
    /// Builds a message (see <see cref="BaseMessage"/>) carrying <paramref name="payload"/> as zlib-compressed JSON in the
    /// <c>CompressionData</c> attribute.
    /// </summary>
    /// <param name="payload">The payload to compress, or <see langword="null"/> to return the base message unchanged.</param>
    /// <param name="groupId">Optional FIFO <c>MessageGroupId</c> attribute.</param>
    /// <param name="withRandomAttributes">Whether to add sample message attributes.</param>
    /// <returns>A new message.</returns>
    public static SQSEvent.SQSMessage WithZlibBody(object? payload = null, string? groupId = null,
                                                   bool withRandomAttributes = true)
    {
        var result = BaseMessage(groupId, withRandomAttributes);

        if (payload is null) return result;

        result.MessageAttributes ??= new Dictionary<string, SQSEvent.MessageAttribute>();
        result.MessageAttributes["CompressionType"] = new SQSEvent.MessageAttribute {
            DataType = "String",
            StringValue = CompressionType.Zlib.ToDescription()
        };

        result.MessageAttributes["CompressionData"] = new SQSEvent.MessageAttribute {
            DataType = "Binary",
            BinaryValue = new MemoryStream(CompressionHelper.CompressFromUtf8(payload.ToJson(), CompressionType.Zlib))
        };

        return result;
    }

    private static Dictionary<string, SQSEvent.MessageAttribute> RandomMessageAttributes(
        Dictionary<string, SQSEvent.MessageAttribute>? messageAttributes = null)
    {
        messageAttributes ??= new Dictionary<string, SQSEvent.MessageAttribute>();

        messageAttributes["StringValue"] =
            new SQSEvent.MessageAttribute { DataType = "String", StringValue = TestHelper.RandomId() };

        messageAttributes["EmptyStringValue"] =
            new SQSEvent.MessageAttribute { DataType = "String", StringValue = string.Empty };

        messageAttributes["NullStringValue"] =
            new SQSEvent.MessageAttribute { DataType = "String", StringValue = null };

        messageAttributes["BinaryValue"] =
            new SQSEvent.MessageAttribute { BinaryValue = TestHelper.RandomMemoryStream() };

        return messageAttributes;
    }
}
