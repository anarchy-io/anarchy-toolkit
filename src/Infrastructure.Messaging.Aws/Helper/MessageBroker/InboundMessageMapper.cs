using Amazon.Lambda.SQSEvents;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker;

public static class InboundMessageMapper
{
    public static ImmutableArray<InboundMessageEntity> FromSqs(SQSEvent sqsEvent)
    {
        return [.. sqsEvent.Records.Select(FromSqs)];
    }

    public static InboundMessageEntity FromSqs(SQSEvent.SQSMessage message)
    {
        return new InboundMessageEntity(
            id: message.MessageId ?? string.Empty,
            receiptHandle: message.ReceiptHandle ?? string.Empty,
            message: ExtractBody(message) ?? string.Empty,
            source: message.EventSourceArn,
            partitionKey: message.Attributes?.GetValueOrDefault("MessageGroupId"),
            attributes: MapStringAttributes(message.MessageAttributes)
        );
    }

    private static string? ExtractBody(SQSEvent.SQSMessage message)
    {
        if (message.MessageAttributes is null ||
            !message.MessageAttributes.TryGetValue("CompressionType", out var typeAttr) ||
            !message.MessageAttributes.TryGetValue("CompressionData", out var dataAttr) ||
            dataAttr?.BinaryValue is not Stream binaryStream)
        {
            return message.Body;
        }

        return typeAttr?.StringValue.ToEnumValue(CompressionType.Uncompressed, caseSensitiveSearch: false) switch {
            CompressionType.Gzip => CompressionHelper.DecompressToUtf8(binaryStream, CompressionType.Gzip),
            CompressionType.Zlib => CompressionHelper.DecompressToUtf8(binaryStream, CompressionType.Zlib),
            _ => message.Body
        };
    }

    public static Dictionary<string, string> MapStringAttributes(
        Dictionary<string, SQSEvent.MessageAttribute>? sqsMessageAttributes)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, val) in sqsMessageAttributes ?? new Dictionary<string, SQSEvent.MessageAttribute>())
        {
            if (val is { DataType: "String", StringValue: not null })
            {
                result[key] = val.StringValue;
            }
        }

        return result;
    }
}
