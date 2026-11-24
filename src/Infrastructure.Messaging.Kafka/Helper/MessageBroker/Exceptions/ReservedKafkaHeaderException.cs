using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helper.MessageBroker.Exceptions;

/// <summary>Thrown when an outbound attribute uses a reserved Kafka header name.</summary>
/// <param name="headerName">The reserved header name.</param>
public class ReservedKafkaHeaderException(string headerName)
    : InvalidArgumentException($"The Kafka header '{headerName}' is reserved.", "message")
{
    /// <summary>The reserved header name.</summary>
    public string HeaderName { get; } = headerName;
}
