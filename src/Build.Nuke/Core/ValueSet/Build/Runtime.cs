using Nuke.Common.Tooling;
using System.ComponentModel;

namespace Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build
{
    /// <summary>
    /// Represents a runtime. Possible values are <see cref="Windows"/> and <see cref="Linux"/>.
    /// </summary>
    [TypeConverter(typeof(TypeConverter<Runtime>))]
    public class Runtime : Enumeration
    {
        public static readonly Runtime Windows = new Runtime { Value = "win10-x64" };
        public static readonly Runtime Linux = new Runtime { Value = "linux-x64" };

        public static implicit operator string(Runtime configuration) => configuration.Value;
    }
}
