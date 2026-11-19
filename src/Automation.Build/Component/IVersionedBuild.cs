using Fallout.Common;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// A <see cref="IDefaultBuild"/> that takes the version being built as the <c>BuildNumber</c> parameter.
/// </summary>
public interface IVersionedBuild : IDefaultBuild
{
    /// <summary>
    /// The version being built (<c>--build-number</c>), typically passed by CI; <see langword="null"/> when not set.
    /// </summary>
    [Parameter("Version to build")]
    string? BuildNumber => TryGetValue(() => BuildNumber);
}
