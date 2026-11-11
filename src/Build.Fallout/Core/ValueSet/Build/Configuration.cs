using Fallout.Common.Tooling;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build
{
    /// <summary>
    /// Represents a build configuration. Possible values are <see cref="Debug"/> and <see cref="Release"/>.
    /// </summary>
    [TypeConverter(typeof(TypeConverter<Configuration>))]
    [JsonConverter(typeof(EnumerationJsonConverter<Configuration>))]
    public class Configuration : Enumeration
    {
        public static Configuration Debug = new Configuration { Value = nameof(Debug) };
        public static Configuration Release = new Configuration { Value = nameof(Release) };

        public static implicit operator string(Configuration configuration) => configuration.Value;
    }
}
