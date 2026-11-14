using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// A <see cref="IDefaultBuild"/> that takes the version being built as the <c>BuildNumber</c> parameter.
/// </summary>
public interface IVersionedBuild : IDefaultBuild
{
    [Parameter("Version to build")]
    string? BuildNumber => TryGetValue(() => BuildNumber);
}
