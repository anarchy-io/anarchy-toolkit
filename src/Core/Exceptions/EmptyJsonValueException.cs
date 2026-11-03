namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a JSON value is null or empty but the target type requires a value.
/// </summary>
public class EmptyJsonValueException(Type targetType)
    : JsonParsingException($"Cannot convert a null or empty JSON value to {targetType.Name}.");
