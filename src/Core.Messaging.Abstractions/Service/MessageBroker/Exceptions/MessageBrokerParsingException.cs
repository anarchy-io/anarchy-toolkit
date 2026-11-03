using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Base for failures parsing an inbound message body.
/// </summary>
public class MessageBrokerParsingException(string message, Exception? innerException = null)
    : InvalidFormatException(message, innerException);
