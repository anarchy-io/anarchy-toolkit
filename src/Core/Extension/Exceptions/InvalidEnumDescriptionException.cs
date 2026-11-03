using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Extension.Exceptions;

/// <summary>
/// Thrown when a string does not match the <see cref="System.ComponentModel.DescriptionAttribute"/> of any value of
/// the target enum.
/// </summary>
public class InvalidEnumDescriptionException(string paramName, string? description, Type enumType)
    : OutOfRangeArgumentException(paramName, description, $"'{description}' is not a valid {enumType.Name} description.")
{
    public Type EnumType { get; } = enumType;
}
