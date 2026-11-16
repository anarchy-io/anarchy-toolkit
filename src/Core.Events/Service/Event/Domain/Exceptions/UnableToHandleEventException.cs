namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

/// <summary>
/// Thrown when a handler fails to process an event.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The exception that caused this one, if any.</param>
public class UnableToHandleEventException(string message, Exception? innerException = null)
    : Exception(message, innerException);
