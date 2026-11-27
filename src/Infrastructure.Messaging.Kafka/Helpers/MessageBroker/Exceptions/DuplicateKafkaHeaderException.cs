namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker.Exceptions;

/// <summary>Thrown when an inbound record contains the same Kafka header name more than once.</summary>
/// <param name="headerName">The duplicated header name.</param>
public class DuplicateKafkaHeaderException(string headerName)
    : KafkaHeaderMappingException(headerName, $"Duplicate Kafka header '{headerName}'.");
