using Amazon.Lambda.SQSEvents;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker;

/// <summary>
/// Maps AWS Lambda SQS events into toolkit <see cref="InboundMessageEntity"/> instances.
/// </summary>
/// <remarks>
/// Bodies compressed by the AWS publishers (<c>CompressionType</c> + binary <c>CompressionData</c> attributes) are
/// decompressed transparently. Only <c>String</c> message attributes are kept. The FIFO <c>MessageGroupId</c> system
/// attribute becomes the partition key.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public async Task Handler(SQSEvent sqsEvent, ILambdaContext context)
/// {
///     foreach (var message in InboundMessageMapper.FromSqs(sqsEvent))
///         await processor.HandleAsync(message);
/// }
/// </code>
/// </example>
public static class InboundMessageMapper
{
    /// <summary>
    /// Maps every record of an SQS event.
    /// </summary>
    /// <param name="sqsEvent">The Lambda SQS event.</param>
    /// <returns>The mapped messages, in record order.</returns>
    public static ImmutableArray<InboundMessageEntity> FromSqs(SQSEvent sqsEvent)
    {
        return [.. sqsEvent.Records.Select(FromSqs)];
    }

    /// <summary>
    /// Maps a single SQS message, decompressing the body when needed.
    /// </summary>
    /// <param name="message">The SQS message.</param>
    /// <returns>The mapped message. Missing id, receipt handle or body are mapped to <see cref="string.Empty"/>.</returns>
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

    /// <summary>
    /// Extracts the <c>String</c>-typed attributes of an SQS message; other data types are skipped.
    /// </summary>
    /// <param name="sqsMessageAttributes">The SQS message attributes, or <see langword="null"/>.</param>
    /// <returns>A new ordinal-keyed dictionary of attribute names to string values (empty when none).</returns>
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
