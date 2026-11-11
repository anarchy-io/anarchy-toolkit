using Anarchy.Toolkit.Build.Fallout.Component;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.CI;
using Fallout.Common.Tools.DotNet;

namespace Build;

/// <summary>
/// The toolkit's build: the standard targets come from <see cref="IStandardBuild"/>
/// (<c>src/Build.Fallout</c>).
/// </summary>
[ShutdownDotNetAfterServerBuild]
[DotNetVerbosityMapping]
internal sealed partial class Build : FalloutBuild, INuGetBuild, IDependencyMapBuild, ILayeringBuild,
                                      IDependencyUpgradeBuild, IGitHooksBuild
{
    /// <summary>The toolkit ships packages only; nothing is deployed.</summary>
    public IReadOnlyCollection<Artifact> Artifacts => [];

    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>()
        .Clean);
}
