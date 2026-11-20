using Fallout.Common.Tooling;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Automation.Build.Core.ValueSet.Build;

/// <summary>
/// Represents a build configuration. Possible values are <see cref="Debug"/> and <see cref="Release"/>.
/// </summary>
/// <remarks>
/// It can be given as a build parameter (<c>--configuration Debug</c>) or in a parameters file, thanks to its
/// <see cref="System.ComponentModel.TypeConverterAttribute"/> and <see cref="EnumerationJsonConverter{T}"/>.
/// </remarks>
[TypeConverter(typeof(TypeConverter<ConfigurationValueSet>))]
[JsonConverter(typeof(EnumerationJsonConverter<ConfigurationValueSet>))]
public class ConfigurationValueSet : Enumeration
{
    /// <summary>
    /// The <c>Debug</c> configuration: unoptimized, with full debugging information. Used by the <c>RunApp</c> and
    /// <c>RunApps</c> targets.
    /// </summary>
    public static ConfigurationValueSet Debug = new() { Value = nameof(Debug) };
    /// <summary>
    /// The <c>Release</c> configuration: optimized. The default of <see cref="Anarchy.Toolkit.Automation.Build.Component.IDefaultBuild.Configuration"/>.
    /// </summary>
    public static ConfigurationValueSet Release = new() { Value = nameof(Release) };

    /// <summary>
    /// Converts the configuration to its MSBuild name, e.g. for <c>SetConfiguration</c>.
    /// </summary>
    /// <param name="configuration">The configuration to convert.</param>
    /// <returns>The configuration's <see cref="Enumeration.Value"/>.</returns>
    public static implicit operator string(ConfigurationValueSet configuration) => configuration.Value;
}
