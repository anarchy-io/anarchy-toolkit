namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker.Exceptions;

/// <summary>Thrown when an inbound Kafka header has a null value.</summary>
/// <param name="headerName">The header name.</param>
public class NullKafkaHeaderException(string headerName)
    : KafkaHeaderMappingException(headerName, $"Kafka header '{headerName}' has a null value.");
