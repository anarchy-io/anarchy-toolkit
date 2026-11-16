namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// Represents an encrypted configuration value.
/// </summary>
/// <remarks>
/// Unlike a plain <see cref="ConfigurationValue"/>, a secret must be created explicitly, so encrypted values are never
/// confused with plain text by an implicit conversion.
/// </remarks>
public sealed class Secret : ConfigurationValue
{
    /// <summary>
    /// Initializes a secret from its encrypted text.
    /// </summary>
    /// <param name="value">The encrypted value.</param>
    public Secret(string value) : base(value)
    {
    }
}
