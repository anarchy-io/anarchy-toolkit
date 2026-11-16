using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Web.Presentation.Attribute;

/// <summary>
/// Validation attribute requiring a non-negative <see langword="int"/> value.
/// </summary>
/// <remarks>Fails for <see langword="null"/>, values of other types and negative numbers.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredIntAttribute : RequiredAttribute
{
    /// <summary>Checks that <paramref name="value"/> is present and is an <see langword="int"/> greater than or equal to zero.</summary>
    /// <param name="value">The value to validate.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) && value is int intValue && intValue >= 0;
    }
}
