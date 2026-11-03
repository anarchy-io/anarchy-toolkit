using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Base for failures publishing outbound messages to a message broker.
/// </summary>
public class MessageBrokerPublisherException(string message, Exception? innerException = null)
    : ExternalDependencyException(message, innerException);
