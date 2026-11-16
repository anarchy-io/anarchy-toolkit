using Anarchy.Toolkit.Core.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Web.Presentation.Attribute;

/// <summary>
/// Validation attribute requiring the value to be a defined member of the given enum type.
/// </summary>
/// <remarks><see langword="null"/> values are considered valid; combine with <see cref="RequiredAttribute"/> if needed.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class DefinedEnumAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    /// <summary>Creates the attribute for <paramref name="enumType"/>.</summary>
    /// <param name="enumType">The enum type the value must belong to.</param>
    /// <exception cref="Anarchy.Toolkit.Core.Exceptions.InvalidEnumTypeException"><paramref name="enumType"/> is not an enum type.</exception>
    public DefinedEnumAttribute(Type enumType)
    {
        if (enumType is not { IsEnum: true })
        {
            throw new InvalidEnumTypeException(nameof(enumType), enumType);
        }

        _enumType = enumType;
        ErrorMessage = "The EnumValue field is not a valid enum value.";
    }

    /// <summary>Checks that <paramref name="value"/> is <see langword="null"/> or defined in the enum type.</summary>
    /// <param name="value">The value to validate.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public override bool IsValid(object? value)
    {
        return value == null || Enum.IsDefined(_enumType, value);
    }
}
