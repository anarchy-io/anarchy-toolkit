using Anarchy.Toolkit.Build.Nuke.Component;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Extension;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Tools.DotNet;

namespace Build;

/// <summary>
/// The toolkit's build: the standard targets come from <see cref="IStandardBuild"/>
/// (<c>src/Build.Nuke</c>); the <c>Build.*.cs</c> files only add what is specific to this repository.
/// </summary>
[ShutdownDotNetAfterServerBuild]
[DotNetVerbosityMapping]
[TelemetryOptout]
internal sealed partial class Build : NukeBuild, INuGetBuild, IDependencyMapBuild, ILayeringBuild,
                                      IDependencyUpgradeBuild, IGitHooksBuild
{
    /// <summary>The toolkit ships packages only; nothing is deployed.</summary>
    public IReadOnlyCollection<Artifact> Artifacts => [];

    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>()
        .Clean);
}
