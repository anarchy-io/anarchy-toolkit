using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helper.MessageBroker.Exceptions;

/// <summary>Thrown when an inbound Kafka header is not valid UTF-8.</summary>
/// <param name="headerName">The header name.</param>
/// <param name="innerException">The original decoder failure.</param>
public class InvalidKafkaHeaderEncodingException(string headerName, DecoderFallbackException innerException)
    : KafkaHeaderMappingException(headerName, $"Kafka header '{headerName}' is not valid UTF-8.", innerException);
