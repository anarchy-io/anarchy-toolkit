using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker;
using Confluent.Kafka;

namespace Anarchy.Toolkit.Testing.Kafka.Fixture;

/// <summary>Builds a consumed Kafka record using the transport's outbound wire mapping.</summary>
public static class KafkaMessageFixture
{
    /// <summary>Creates a record at the supplied partition and offset.</summary>
    public static ConsumeResult<string, string> FromOutbound(OutboundMessage message, int partition = 0, long offset = 0)
    {
        if (message is null) throw new KafkaNullArgumentException(nameof(message));
        return new ConsumeResult<string, string> {
            Topic = message.Destination,
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = KafkaMessageMapper.ToKafka(message)
        };
    }
}
