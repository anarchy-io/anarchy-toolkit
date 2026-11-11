
namespace Anarchy.Toolkit.Build.Fallout.Core.Entity.Build
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
