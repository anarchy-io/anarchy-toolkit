using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker.Exceptions;

/// <summary>Thrown when a Kafka result has no message or represents a partition-end notification.</summary>
public class KafkaPartitionEndException()
    : InvalidArgumentException("A Kafka result without a message, including a partition-end notification, cannot be mapped.", "result");
