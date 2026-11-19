using Anarchy.Toolkit.Automation.Build.Component;
using Anarchy.Toolkit.Automation.Build.Core.Entity.Build;
using Anarchy.Toolkit.Automation.Build.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.CI;
using Fallout.Common.Tools.DotNet;

namespace Build;

/// <summary>
/// The toolkit's build: the standard targets come from <see cref="IStandardBuild"/>
/// (<c>src/Automation.Build</c>).
/// </summary>
[ShutdownDotNetAfterServerBuild]
[DotNetVerbosityMapping]
internal sealed partial class Build : FalloutBuild, INuGetBuild, IDependencyMapBuild, ILayeringBuild,
                                      IDependencyUpgradeBuild, IGitHooksBuild, IDockerBuild, IMockServerBuild,
                                      IApiReferenceBuild, IAgentSkillsBuild
{
    /// <summary>The toolkit ships packages only; nothing is deployed.</summary>
    public IReadOnlyCollection<Artifact> Artifacts => [];

    /// <summary>The dependency map uses the same layer names as this repository's layering configuration.</summary>
    IReadOnlyList<string> IDependencyMapBuild.DependencyMapLayers =>
        ["Core", "Infrastructure", "Configuration", "Hosting", "Web", "Automation"];

    /// <summary>The repository's own skills share the toolkit's prefix.</summary>
    string IAgentSkillsBuild.AgentSkillsPrefix => "anarchy";

    /// <summary>
    /// The repository's own skills, then the distributed ones; the root folder links to the latter so agents find both.
    /// </summary>
    string[] IAgentSkillsBuild.AgentSkillsDirectories => [".agents/skills", "src/Automation/Boilerplate/.agents/skills"];

    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>()
        .Clean);
}
