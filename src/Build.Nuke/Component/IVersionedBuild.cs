using Nuke.Common;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    public interface IVersionedBuild : IDefaultBuild
    {
        [Parameter("Version to build")]
        string? BuildNumber => TryGetValue(() => BuildNumber);
    }
}
