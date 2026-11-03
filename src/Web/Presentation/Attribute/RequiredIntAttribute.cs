using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Web.Presentation.Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredIntAttribute : RequiredAttribute
{
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) && value is int intValue && intValue >= 0;
    }
}
