namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// <para>
/// Represents a configuration value. Used as-is, it represents a plain text configuration value, as opposed to
/// <see cref="Secret"/>, which inherits from <see cref="ConfigurationValue"/> but represents an encrypted value.
/// </para>
/// <para>
/// <see cref="string"/> can be implicitly converted to <see cref="ConfigurationValue"/>.
/// </para>
/// </summary>
public class ConfigurationValue
{
    /// <summary>
    /// The raw value: plain text, or the encrypted text for a <see cref="Secret"/>.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Wraps a plain text <see cref="string"/> into a <see cref="ConfigurationValue"/>.
    /// </summary>
    /// <param name="value">The plain text value.</param>
    /// <returns>A plain (non-secret) <see cref="ConfigurationValue"/> holding <paramref name="value"/>.</returns>
    public static implicit operator ConfigurationValue(string value) => new(value);

    /// <summary>
    /// Initializes a configuration value; derived types (such as <see cref="Secret"/>) give it their own meaning.
    /// </summary>
    /// <param name="value">The raw value.</param>
    protected ConfigurationValue(string value) => Value = value;
}
