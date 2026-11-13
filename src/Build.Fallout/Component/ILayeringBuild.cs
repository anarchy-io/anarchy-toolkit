using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Solutions;
using Serilog;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    /// <summary>
    /// Enforces the solution's layered architecture, where a project's layer is the prefix of its name by default,
    /// or its folder under <see cref="LayeringRoot"/> when <see cref="LayeringByFolder"/> is set: references, imports and folders may only point
    /// to the project's own or a lower layer. The <c>Testing</c> kits may reference anything, but nothing in a layer may
    /// reference them. See <see cref="LayeringTasks"/> for the exact rules.
    /// </summary>
    public interface ILayeringBuild : IStandardBuild
    {
        /// <summary>The layered chain, innermost first. By default the toolkit's one (see the architecture guide).</summary>
        [Parameter("Layers, innermost first (defaults to the toolkit's: Core Infrastructure Configuration Hosting Web Build)")]
        string[] LayeringLayers => TryGetValue(() => LayeringLayers) ?? LayeringTasks.DefaultLayers.ToArray();

        /// <summary>Root of projects to check, relative to the solution directory (or an absolute path).</summary>
        [Parameter("Root folder for layering checks, relative to the solution (defaults to src)")]
        string LayeringRoot => TryGetValue(() => LayeringRoot) ?? "src";

        /// <summary>Use the first folder under the layering root as the layer instead of the project name prefix.</summary>
        [Parameter("Determine project layers from folders under the layering root instead of project name prefixes")]
        bool LayeringByFolder => TryGetValue<bool?>(() => LayeringByFolder) ?? false;

        /// <summary>
        /// Folders whose projects all belong to one layer, as <c>folder=Layer</c> relative to the solution (e.g.
        /// <c>src/Api=Web</c>), checked before <see cref="LayeringRoot"/>: applications grouped by kind rather than layer.
        /// </summary>
        [Parameter("Folders whose projects belong to one layer, as folder=Layer (e.g. src/Api=Web)")]
        string[] LayeringApplicationFolders => TryGetValue(() => LayeringApplicationFolders) ?? [];

        /// <summary>
        /// The layer of a project, or <c>null</c> to leave it out of the check. By default, projects under <see cref="LayeringRoot"/>
        /// use their name prefix, or their first folder under that root if <see cref="LayeringByFolder"/> is set; projects in
        /// one of the <see cref="LayeringApplicationFolders"/> get that folder's layer.
        /// An unknown layer is a violation.
        /// </summary>
        string? GetLayer(Project project)
        {
            var root = Path.GetRelativePath(Solution.Directory, Path.GetFullPath(LayeringRoot, Solution.Directory));
            return LayeringTasks.GetLayer(project.Name, RelativePath(project), root, LayeringByFolder, LayeringApplicationFolders);
        }

        Target CheckLayering => d => d
            .Description("Verifies the layered architecture: project references, file imports and layer folders")
            .Executes(() => {
                var solutionProjects = Solution.ToProjectInfos(RelativePath);
                var projects = LayeringTasks.Collect(solutionProjects.Keys, info => GetLayer(solutionProjects[info]));
                var violations = LayeringTasks.Check(projects, LayeringLayers, LayeringByFolder);

                if (violations.Count > 0)
                {
                    foreach (var violation in violations)
                    {
                        Log.Error("{Violation}", violation);
                    }

                    Assert.Fail($"{violations.Count} layering violations");
                }

                Log.Information("Layering OK across {Projects} projects: {Layers} (projects, files and folders)",
                    projects.Count, string.Join(" < ", LayeringLayers));
            });
    }
}
