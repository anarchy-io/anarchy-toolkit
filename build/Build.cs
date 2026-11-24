using Anarchy.Toolkit.Automation.Build.Component;
using Anarchy.Toolkit.Automation.Build.Core.Entity.Build;
using Anarchy.Toolkit.Automation.Build.Infrastructure.Extension;
using Anarchy.Toolkit.Automation.Core.Service;
using Fallout.Common;
using Fallout.Common.CI;
using Fallout.Common.Tools.DotNet;
using Serilog;

namespace Build;

/// <summary>
/// The toolkit's build: the standard targets come from <see cref="IStandardBuild"/>
/// (<c>src/Automation.Build</c>).
/// </summary>
[ShutdownDotNetAfterServerBuild]
[DotNetVerbosityMapping]
internal sealed partial class Build : AnarchyBuild, INuGetBuild, IDependencyMapBuild, ILayeringBuild,
                                      IDependencyUpgradeBuild, IGitHooksBuild, ITestEnvironmentBuild, IMockServerBuild,
                                      IApiReferenceBuild, IAgentSkillsBuild
{
    /// <summary>The toolkit ships packages only; nothing is deployed.</summary>
    public IReadOnlyCollection<Artifact> Artifacts => [];

    /// <summary>
    /// Checks the toolkit's package-name hierarchy (see <see cref="PackageHierarchy"/>) on top of the layering, for
    /// this repository only, logging every violation and failing the build when there is any.
    /// </summary>
    public Target CheckPackageHierarchy => d => d
        .Description("Verifies the toolkit's package hierarchy: same-layer references only to name ancestors or allowed pairs")
        .TryDependsOn<ILayeringBuild>(x => x.CheckLayering)
        .Executes(() => {
            var layering = this.As<ILayeringBuild>();
            var layout = layering.Layering;
            var solutionProjects = layering.Solution.ToProjectInfos(layering.RelativePath);
            var projects = LayeringTasks.Collect(solutionProjects.Keys, info => layering.GetLayer(solutionProjects[info], layout));
            var violations = PackageHierarchy.Check(projects, layout.Layers, PackageHierarchy.AllowedReferences);

            if (violations.Count > 0)
            {
                foreach (var violation in violations)
                {
                    Log.Error("{Violation}", violation);
                }

                Assert.Fail($"{violations.Count} package hierarchy violations");
            }

            Log.Information("Package hierarchy OK across {Projects} projects ({Allowed} allowed cross-family references)",
                projects.Count, PackageHierarchy.AllowedReferences.Count);
        });

    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>()
        .Clean);
}
