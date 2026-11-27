namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker.Exceptions;

/// <summary>Thrown when an outbound Kafka attribute has a null value.</summary>
/// <param name="headerName">The attribute's header name.</param>
public class NullKafkaAttributeException(string headerName)
    : ArgumentNullException("message", $"Kafka attribute '{headerName}' has a null value.")
{
    /// <summary>The attribute's header name.</summary>
    public string HeaderName { get; } = headerName;
}
