namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for every argument validation failure. Wraps <see cref="ArgumentException"/> so callers relying on the
/// native type keep working.
/// </summary>
public class InvalidArgumentException : ArgumentException
{
    public InvalidArgumentException()
    {
    }

    public InvalidArgumentException(string? message, string? paramName = null, Exception? innerException = null)
        : base(message, paramName, innerException)
    {
    }

    public InvalidArgumentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
