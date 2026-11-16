using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Web.Presentation.Attribute;

/// <summary>
/// Validation attribute requiring a <see langword="decimal"/> value.
/// </summary>
/// <remarks>Fails for <see langword="null"/> and values of other types.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredDecimalAttribute : RequiredAttribute
{
    /// <summary>Checks that <paramref name="value"/> is present and is a <see langword="decimal"/>.</summary>
    /// <param name="value">The value to validate.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) && value is decimal;
    }
}
