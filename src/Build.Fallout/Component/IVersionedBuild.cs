using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    public interface IVersionedBuild : IDefaultBuild
    {
        [Parameter("Version to build")]
        string? BuildNumber => TryGetValue(() => BuildNumber);
    }
}
