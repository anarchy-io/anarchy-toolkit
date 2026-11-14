using Fallout.Common.Tooling;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build;

/// <summary>
/// Represents a runtime. Possible values are <see cref="Windows"/> and <see cref="Linux"/>.
/// </summary>
[TypeConverter(typeof(TypeConverter<Runtime>))]
[JsonConverter(typeof(EnumerationJsonConverter<Runtime>))]
public class Runtime : Enumeration
{
    /// <summary>
    /// The 64-bit Windows runtime identifier, <c>win10-x64</c>.
    /// </summary>

    public static readonly Runtime Windows = new() { Value = "win10-x64" };

    /// <summary>
    /// The 64-bit Linux runtime identifier, <c>linux-x64</c>.
    /// </summary>
    public static readonly Runtime Linux = new() { Value = "linux-x64" };

    public static implicit operator string(Runtime configuration) => configuration.Value;
}
