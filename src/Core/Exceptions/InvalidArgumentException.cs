namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for every argument validation failure. Wraps <see cref="ArgumentException"/> so callers relying on the
/// native type keep working.
/// </summary>
public class InvalidArgumentException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidArgumentException"/> class with the default message.
    /// </summary>
    public InvalidArgumentException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidArgumentException"/> class.
    /// </summary>
    /// <param name="message">A message describing the validation failure.</param>
    /// <param name="paramName">The name of the invalid argument.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public InvalidArgumentException(string? message, string? paramName = null, Exception? innerException = null)
        : base(message, paramName, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidArgumentException"/> class without a parameter name.
    /// </summary>
    /// <param name="message">A message describing the validation failure.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public InvalidArgumentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
