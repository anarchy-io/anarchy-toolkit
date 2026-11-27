using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Runtime.Web.Presentation.Attribute;

/// <summary>
/// Validation attribute requiring a <see langword="long"/> value.
/// </summary>
/// <remarks>Fails for <see langword="null"/> and values of other types.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredLongAttribute : RequiredAttribute
{
    /// <summary>Checks that <paramref name="value"/> is present and is a <see langword="long"/>.</summary>
    /// <param name="value">The value to validate.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) && value is long;
    }
}
