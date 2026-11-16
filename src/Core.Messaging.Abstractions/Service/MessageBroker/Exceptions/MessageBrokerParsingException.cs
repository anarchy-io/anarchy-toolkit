using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Base for failures parsing an inbound message body.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying parsing failure, if any.</param>
public class MessageBrokerParsingException(string message, Exception? innerException = null)
    : InvalidFormatException(message, innerException);
