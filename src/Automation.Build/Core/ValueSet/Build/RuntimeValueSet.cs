using Fallout.Common.Tooling;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Automation.Build.Core.ValueSet.Build;

/// <summary>
/// Represents a runtime. Possible values are <see cref="Windows"/> and <see cref="Linux"/>.
/// </summary>
[TypeConverter(typeof(TypeConverter<RuntimeValueSet>))]
[JsonConverter(typeof(EnumerationJsonConverter<RuntimeValueSet>))]
public class RuntimeValueSet : Enumeration
{
    /// <summary>
    /// The 64-bit Windows runtime identifier, <c>win10-x64</c>.
    /// </summary>

    public static readonly RuntimeValueSet Windows = new() { Value = "win10-x64" };

    /// <summary>
    /// The 64-bit Linux runtime identifier, <c>linux-x64</c>.
    /// </summary>
    public static readonly RuntimeValueSet Linux = new() { Value = "linux-x64" };

    /// <summary>
    /// Converts the runtime to its runtime identifier (RID).
    /// </summary>
    /// <param name="configuration">The runtime to convert.</param>
    /// <returns>The runtime identifier, e.g. <c>linux-x64</c>.</returns>
    public static implicit operator string(RuntimeValueSet configuration) => configuration.Value;
}
