using Fallout.Common.Tooling;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build;

/// <summary>
/// Represents an AWS environment. Possible values are <see cref="Testing"/>, <see cref="Acceptance"/>,
/// <see cref="Production"/> and <see cref="Shared"/>. Values compare case-insensitively.
/// </summary>
[TypeConverter(typeof(TypeConverter<Environment>))]
[JsonConverter(typeof(EnumerationJsonConverter<Environment>))]
public sealed class Environment : Enumeration
{
    public static readonly Environment Testing = new() { Value = nameof(Testing) };
    public static readonly Environment Acceptance = new() { Value = nameof(Acceptance) };
    public static readonly Environment Production = new() { Value = nameof(Production) };
    public static readonly Environment Shared = new() { Value = nameof(Shared) };

    public static implicit operator string(Environment environment) => environment.Value;

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

    public static bool operator !=(Environment? a, Environment? b) => !(a == b);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return ReferenceEquals(this, obj)
               || obj.GetType() == GetType() && Equals((Environment)obj);
    }

    private bool Equals(Environment other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => base.GetHashCode();
}
