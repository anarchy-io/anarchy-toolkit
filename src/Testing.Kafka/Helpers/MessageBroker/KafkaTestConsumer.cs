using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker;
using Confluent.Kafka;

namespace Anarchy.Toolkit.Testing.Kafka.Helpers.MessageBroker;

/// <summary>A real Kafka consumer for reading a known partition in integration tests.</summary>
/// <remarks>Use a unique topic per test. This helper does not create, delete, or purge topics or explicitly commit offsets; disable auto commit in the configuration.</remarks>
public sealed class KafkaTestConsumer : IDisposable
{
    private readonly IConsumer<string, string> _consumer;

    /// <summary>Creates a consumer; the caller owns the broker and supplies its configuration and group ID.</summary>
    public KafkaTestConsumer(ConsumerConfig config, string topic, int partition = 0)
    {
        if (config is null) throw new KafkaNullArgumentException(nameof(config));
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        try
        {
            _consumer.Assign(new TopicPartitionOffset(topic, new Partition(partition), Offset.Beginning));
        }
        catch
        {
            _consumer.Dispose();
            throw;
        }
    }

    /// <summary>Reads and maps one record, bounded by the caller's cancellation token.</summary>
    public InboundMessage Read(CancellationToken cancellationToken) => KafkaMessageMapper.FromKafka(_consumer.Consume(cancellationToken));

    /// <summary>Closes and disposes the owned consumer.</summary>
    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }
}
