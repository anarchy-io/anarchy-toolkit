using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Web.Presentation.Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredDecimalAttribute : RequiredAttribute
{
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) && value is decimal;
    }
}
