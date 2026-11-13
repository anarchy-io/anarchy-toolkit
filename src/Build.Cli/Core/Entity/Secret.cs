namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// Represents an encrypted configuration value.
/// </summary>
public sealed class Secret : ConfigurationValue
{
    public Secret(string value) : base(value)
    {
    }
}
