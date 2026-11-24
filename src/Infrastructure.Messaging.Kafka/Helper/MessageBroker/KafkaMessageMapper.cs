using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helper.MessageBroker.Exceptions;
using Confluent.Kafka;
using System.Globalization;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helper.MessageBroker;

/// <summary>Maps the toolkit's string messages to Kafka records and back.</summary>
public static class KafkaMessageMapper
{
    /// <summary>Reserved header carrying the outbound message identifier.</summary>
    public const string IdHeader = "anarchy-message-id";
    /// <summary>Reserved header carrying the optional deduplication identifier (not enforced by Kafka).</summary>
    public const string DeduplicationIdHeader = "anarchy-deduplication-id";
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    /// <summary>Maps a message; reserved header names cannot be used as custom attributes.</summary>
    public static Message<string, string> ToKafka(OutboundMessage message)
    {
        if (message is null) throw new KafkaNullArgumentException(nameof(message));
        var headers = new Headers { { IdHeader, Utf8.GetBytes(message.Id) } };
        if (message.DeduplicationId is not null)
            headers.Add(DeduplicationIdHeader, Utf8.GetBytes(message.DeduplicationId));

        foreach (var (key, value) in message.Attributes)
        {
            if (key is IdHeader or DeduplicationIdHeader)
                throw new ReservedKafkaHeaderException(key);
            if (value is null)
                throw new NullKafkaAttributeException(key);
            headers.Add(key, Utf8.GetBytes(value));
        }

        return new Message<string, string> { Key = message.PartitionKey!, Value = message.Message, Headers = headers };
    }

    /// <summary>Maps a consumed record. External records without an ID use their topic, partition and offset as ID.</summary>
    /// <remarks>Headers must be unique, non-null UTF-8 strings. Kafka tombstones and empty bodies are not supported.</remarks>
    public static InboundMessage FromKafka(ConsumeResult<string, string> result)
    {
        if (result is null) throw new KafkaNullArgumentException(nameof(result));
        if (result.IsPartitionEOF || result.Message is null)
            throw new KafkaPartitionEndException();
        var reference = string.Create(CultureInfo.InvariantCulture, $"{result.Topic}:{result.Partition.Value}:{result.Offset.Value}");
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        string? id = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var header in result.Message.Headers ?? new Headers())
        {
            if (!seen.Add(header.Key))
                throw new DuplicateKafkaHeaderException(header.Key);
            var bytes = header.GetValueBytes() ?? throw new NullKafkaHeaderException(header.Key);
            string value;
            try
            {
                value = Utf8.GetString(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                throw new InvalidKafkaHeaderEncodingException(header.Key, ex);
            }
            if (header.Key == IdHeader)
                id = value;
            else if (header.Key != DeduplicationIdHeader)
                attributes.Add(header.Key, value);
        }

        return new InboundMessage(id ?? reference, reference, result.Message.Value, result.Topic,
            result.Message.Key, attributes);
    }
}
