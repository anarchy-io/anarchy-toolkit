namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when an enum value is not defined by its enum type.
/// </summary>
public class UndefinedEnumValueException(string paramName, Enum value)
    : OutOfRangeArgumentException(paramName, value, $"'{value}' is not a defined {value.GetType().Name} value.")
{
    public Type EnumType { get; } = value.GetType();
}
