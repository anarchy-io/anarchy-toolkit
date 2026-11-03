namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for arguments outside the set of accepted values. Wraps <see cref="ArgumentOutOfRangeException"/>.
/// </summary>
public class OutOfRangeArgumentException : ArgumentOutOfRangeException
{
    public OutOfRangeArgumentException(string? paramName, object? actualValue, string? message)
        : base(paramName, actualValue, message)
    {
    }
}
