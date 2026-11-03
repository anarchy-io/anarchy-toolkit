using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Extension.Exceptions;

/// <summary>
/// Thrown when an enum value has no <see cref="System.ComponentModel.DescriptionAttribute"/>.
/// </summary>
public class MissingEnumDescriptionException(Enum value)
    : NotFoundException($"{value.GetType().Name}.{value} does not have a description.")
{
    public Enum Value { get; } = value;
}
