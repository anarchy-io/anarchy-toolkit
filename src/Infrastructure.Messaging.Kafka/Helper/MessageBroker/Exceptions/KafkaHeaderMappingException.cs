using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helper.MessageBroker.Exceptions;

/// <summary>Base for invalid Kafka headers encountered while mapping a consumed record.</summary>
/// <param name="headerName">The invalid header's name.</param>
/// <param name="message">The specific failure description.</param>
/// <param name="innerException">The underlying decoding failure, if any.</param>
public abstract class KafkaHeaderMappingException(string headerName, string message, Exception? innerException = null)
    : MessageBrokerParsingException(message, innerException)
{
    /// <summary>The invalid header's name.</summary>
    public string HeaderName { get; } = headerName;
}
