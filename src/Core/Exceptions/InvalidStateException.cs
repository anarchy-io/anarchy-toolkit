namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for operations that cannot run in the current object or environment state. Wraps
/// <see cref="InvalidOperationException"/> so callers relying on the native type keep working.
/// </summary>
/// <param name="message">A message describing the invalid state.</param>
/// <param name="innerException">The exception that caused this one, if any.</param>
public class InvalidStateException(string? message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
