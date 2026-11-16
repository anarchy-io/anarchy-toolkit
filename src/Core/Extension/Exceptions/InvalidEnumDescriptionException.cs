using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Extension.Exceptions;

/// <summary>
/// Thrown when a string does not match the <see cref="System.ComponentModel.DescriptionAttribute"/> of any value of
/// the target enum.
/// </summary>
/// <remarks>Raised by the <c>ToEnumValue</c> extensions in <see cref="EnumExtension"/>.</remarks>
/// <param name="paramName">The name of the argument holding the description.</param>
/// <param name="description">The description that could not be matched.</param>
/// <param name="enumType">The enum type that was searched.</param>
public class InvalidEnumDescriptionException(string paramName, string? description, Type enumType)
    : OutOfRangeArgumentException(paramName, description, $"'{description}' is not a valid {enumType.Name} description.")
{
    /// <summary>
    /// Gets the enum type that was searched for a matching description.
    /// </summary>
    public Type EnumType { get; } = enumType;
}
