namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

/// <summary>
/// Thrown when a handler fails to process an event.
/// </summary>
public class UnableToHandleEventException(string message, Exception? innerException = null)
    : Exception(message, innerException);
