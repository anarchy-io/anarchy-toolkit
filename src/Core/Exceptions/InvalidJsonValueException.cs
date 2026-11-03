namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a JSON value is well-formed but not a valid value of the target type.
/// </summary>
public class InvalidJsonValueException(string? value, Type targetType, Exception? innerException = null)
    : JsonParsingException($"\"{value}\" is not a valid {targetType.Name} value.", innerException);
