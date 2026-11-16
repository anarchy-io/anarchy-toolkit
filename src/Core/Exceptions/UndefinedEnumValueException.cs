namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when an enum value is not defined by its enum type.
/// </summary>
/// <param name="paramName">The name of the argument holding the value.</param>
/// <param name="value">The undefined enum value.</param>
public class UndefinedEnumValueException(string paramName, Enum value)
    : OutOfRangeArgumentException(paramName, value, $"'{value}' is not a defined {value.GetType().Name} value.")
{
    /// <summary>
    /// Gets the enum type of the undefined value.
    /// </summary>
    public Type EnumType { get; } = value.GetType();
}
