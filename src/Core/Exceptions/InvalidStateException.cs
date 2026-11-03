namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for operations that cannot run in the current object or environment state. Wraps
/// <see cref="InvalidOperationException"/> so callers relying on the native type keep working.
/// </summary>
public class InvalidStateException(string? message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
