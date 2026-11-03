using Nuke.Common.Tooling;
using System.ComponentModel;

namespace Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build
{
    /// <summary>
    /// Represents an AWS environment. Possible values are <see cref="Testing"/>, <see cref="Acceptance"/>, and
    /// <see cref="Production"/>.
    /// </summary>
    [TypeConverter(typeof(TypeConverter<Environment>))]
    public sealed class Environment : Enumeration
    {
        public static readonly Environment Testing = new Environment { Value = nameof(Testing) };
        public static readonly Environment Acceptance = new Environment { Value = nameof(Acceptance) };
        public static readonly Environment Production = new Environment { Value = nameof(Production) };
        public static readonly Environment Shared = new Environment { Value = nameof(Shared) };

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
}
