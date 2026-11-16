namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for values that cannot be parsed into, or formatted as, a target representation. Wraps
/// <see cref="FormatException"/> so callers relying on the native type keep working.
/// </summary>
public class InvalidFormatException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFormatException"/> class for a value that cannot be
    /// converted to <paramref name="targetType"/>.
    /// </summary>
    /// <param name="value">The value that could not be converted.</param>
    /// <param name="targetType">The type the value was being converted to.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public InvalidFormatException(object? value, Type targetType, Exception? innerException = null)
        : base($"Cannot convert '{value}' ({value?.GetType().Name ?? "null"}) to {targetType.Name}.", innerException)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFormatException"/> class with a custom message, for use by
    /// derived exceptions. <see cref="TargetType"/> is left <see langword="null"/>.
    /// </summary>
    /// <param name="message">A message describing the failure.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    protected InvalidFormatException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the type the value was being converted to, when known.
    /// </summary>
    public Type? TargetType { get; }
}
