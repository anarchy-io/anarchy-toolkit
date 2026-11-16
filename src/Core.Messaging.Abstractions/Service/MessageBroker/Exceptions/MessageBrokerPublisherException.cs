using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Base for failures publishing outbound messages to a message broker.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying broker failure, if any.</param>
public class MessageBrokerPublisherException(string message, Exception? innerException = null)
    : ExternalDependencyException(message, innerException);
