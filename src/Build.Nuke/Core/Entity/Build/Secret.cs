
namespace Anarchy.Toolkit.Build.Nuke.Core.Entity.Build
{
    /// <summary>
    /// Represents an encrypted configuration value.
    /// </summary>
    public sealed class Secret : ConfigurationValue
    {
        public Secret(string value) : base(value)
        {
        }
    }
}
