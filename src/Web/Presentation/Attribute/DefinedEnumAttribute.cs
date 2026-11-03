using Anarchy.Toolkit.Core.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Web.Presentation.Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class DefinedEnumAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    public DefinedEnumAttribute(Type enumType)
    {
        if (enumType is not { IsEnum: true })
        {
            throw new InvalidEnumTypeException(nameof(enumType), enumType);
        }

        _enumType = enumType;
        ErrorMessage = "The EnumValue field is not a valid enum value.";
    }

    public override bool IsValid(object? value)
    {
        return value == null || Enum.IsDefined(_enumType, value);
    }
}
