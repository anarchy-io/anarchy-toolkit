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
public class SqsMessageFixture
{
    public const string DefaultAwsRegion = "eu-west-1";
    public const string DefaultEventSource = "aws:sqs";
    public const string DefaultAccount = "123456789012";
    public const string DefaultQueueName = "fixture-queue.fifo";

    public const string DefaultEventSourceArn =
        $"arn:{DefaultEventSource}:{DefaultAwsRegion}:{DefaultAccount}:{DefaultQueueName}";

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

    public static SQSEvent.SQSMessage WithUncompressedBody(object? payload = null, string? groupId = null,
                                                           bool withRandomAttributes = true)
    {
        var result = BaseMessage(groupId, withRandomAttributes);

        result.Body = payload?.ToJson();
        result.Md5OfBody = payload is null ? null : HashHelper.HashWithMd5(payload);

        return result;
    }

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
