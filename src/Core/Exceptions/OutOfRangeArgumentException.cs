namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for arguments outside the set of accepted values. Wraps <see cref="ArgumentOutOfRangeException"/>.
/// </summary>
public class OutOfRangeArgumentException : ArgumentOutOfRangeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutOfRangeArgumentException"/> class.
    /// </summary>
    /// <param name="paramName">The name of the argument.</param>
    /// <param name="actualValue">The rejected value.</param>
    /// <param name="message">A message describing the failure.</param>
    public OutOfRangeArgumentException(string? paramName, object? actualValue, string? message)
        : base(paramName, actualValue, message)
    {
    }
}
