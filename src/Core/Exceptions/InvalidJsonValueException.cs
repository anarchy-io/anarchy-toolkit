namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a JSON value is well-formed but not a valid value of the target type.
/// </summary>
/// <param name="value">The raw JSON value that could not be converted.</param>
/// <param name="targetType">The type the value was being converted to.</param>
/// <param name="innerException">The exception that caused this one, if any.</param>
public class InvalidJsonValueException(string? value, Type targetType, Exception? innerException = null)
    : JsonParsingException($"\"{value}\" is not a valid {targetType.Name} value.", innerException);
