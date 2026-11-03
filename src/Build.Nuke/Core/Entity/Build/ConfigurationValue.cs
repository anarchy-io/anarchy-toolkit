
namespace Anarchy.Toolkit.Build.Nuke.Core.Entity.Build
{
    /// <summary>
    /// <para>
    /// Represents a configuration value. Used as-is, it represents a plain text configuration value, as opposed to
    /// <see cref="Secret"/>, which inherits from <see cref="ConfigurationValue"/>, but represents an encrypted value.
    /// </para>
    /// <para>
    /// <see cref="string"/> can be implicitly converted to <see cref="ConfigurationValue"/>.
    /// </para>
    /// </summary>
    public class ConfigurationValue
    {
        public string Value { get; }

        public static implicit operator ConfigurationValue(string value) => new ConfigurationValue(value);

        protected ConfigurationValue(string value) => Value = value;
    }
}
