namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for values that cannot be parsed into, or formatted as, a target representation. Wraps
/// <see cref="FormatException"/> so callers relying on the native type keep working.
/// </summary>
public class InvalidFormatException : FormatException
{
    public InvalidFormatException(object? value, Type targetType, Exception? innerException = null)
        : base($"Cannot convert '{value}' ({value?.GetType().Name ?? "null"}) to {targetType.Name}.", innerException)
    {
        TargetType = targetType;
    }

    protected InvalidFormatException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public Type? TargetType { get; }
}
