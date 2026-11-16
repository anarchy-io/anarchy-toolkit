using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Extension.Exceptions;

/// <summary>
/// Thrown when an enum value has no <see cref="System.ComponentModel.DescriptionAttribute"/>.
/// </summary>
/// <remarks>Raised by <see cref="EnumExtension.ToDescription{T}(T)"/>.</remarks>
/// <param name="value">The enum value that lacks a description.</param>
public class MissingEnumDescriptionException(Enum value)
    : NotFoundException($"{value.GetType().Name}.{value} does not have a description.")
{
    /// <summary>
    /// Gets the enum value that lacks a <see cref="System.ComponentModel.DescriptionAttribute"/>.
    /// </summary>
    public Enum Value { get; } = value;
}
