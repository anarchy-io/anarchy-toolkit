using Fallout.Common.Tooling;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build;

/// <summary>
/// Represents an AWS environment. Possible values are <see cref="Testing"/>, <see cref="Acceptance"/>,
/// <see cref="Production"/> and <see cref="Shared"/>. Values compare case-insensitively.
/// </summary>
/// <remarks>
/// <see cref="Anarchy.Toolkit.Build.Fallout.Component.IDefaultBuild.BuildEnvironment"/> reads it from the
/// <c>ASPNETCORE_ENVIRONMENT</c> or <c>DOTNET_ENVIRONMENT</c> parameter (typically an environment variable).
/// </remarks>
[TypeConverter(typeof(TypeConverter<Environment>))]
[JsonConverter(typeof(EnumerationJsonConverter<Environment>))]
public sealed class Environment : Enumeration
{
    /// <summary>
    /// The <c>Testing</c> environment.
    /// </summary>
    public static readonly Environment Testing = new() { Value = nameof(Testing) };
    /// <summary>
    /// The <c>Acceptance</c> environment.
    /// </summary>
    public static readonly Environment Acceptance = new() { Value = nameof(Acceptance) };
    /// <summary>
    /// The <c>Production</c> environment.
    /// </summary>
    public static readonly Environment Production = new() { Value = nameof(Production) };
    /// <summary>
    /// The <c>Shared</c> environment, for resources shared by the other environments.
    /// </summary>
    public static readonly Environment Shared = new() { Value = nameof(Shared) };

    /// <summary>
    /// Converts the environment to its name.
    /// </summary>
    /// <param name="environment">The environment to convert.</param>
    /// <returns>The environment's <see cref="Enumeration.Value"/>.</returns>
    public static implicit operator string(Environment environment) => environment.Value;

    /// <summary>
    /// Compares two environments by value, case-insensitively.
    /// </summary>
    /// <param name="a">The first environment, or <see langword="null"/>.</param>
    /// <param name="b">The second environment, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both are <see langword="null"/> or have the same value; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(Environment? a, Environment? b)
    {
        if (ReferenceEquals(a, null)
            && ReferenceEquals(b, null))
            return true;
        if (ReferenceEquals(a, null)
            || ReferenceEquals(b, null))
            return false;

        return a.Equals(b);
    }

    /// <summary>
    /// Compares two environments by value, case-insensitively.
    /// </summary>
    /// <param name="a">The first environment, or <see langword="null"/>.</param>
    /// <param name="b">The second environment, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the environments differ; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(Environment? a, Environment? b) => !(a == b);

    /// <summary>
    /// Determines whether <paramref name="obj"/> is an <see cref="Environment"/> of exactly this type with the same
    /// value, compared case-insensitively.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return ReferenceEquals(this, obj)
               || obj.GetType() == GetType() && Equals((Environment)obj);
    }

    private bool Equals(Environment other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode() => base.GetHashCode();
}
