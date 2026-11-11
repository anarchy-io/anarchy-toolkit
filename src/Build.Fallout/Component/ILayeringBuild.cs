using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
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
        IReadOnlyList<string> LayeringLayers => LayeringTasks.DefaultLayers;

        /// <summary>Root of projects to check, relative to the solution directory (or an absolute path).</summary>
        [Parameter("Root folder for layering checks, relative to the solution (defaults to src)")]
        string LayeringRoot => TryGetValue(() => LayeringRoot) ?? "src";

        /// <summary>Use the first folder under the layering root as the layer instead of the project name prefix.</summary>
        [Parameter("Determine project layers from folders under the layering root instead of project name prefixes")]
        bool LayeringByFolder => TryGetValue<bool?>(() => LayeringByFolder) ?? false;

        /// <summary>
        /// The layer of a project, or <c>null</c> to leave it out of the check. By default, projects under <see cref="LayeringRoot"/>
        /// use their name prefix, or their first folder under that root if <see cref="LayeringByFolder"/> is set.
        /// An unknown layer is a violation.
        /// </summary>
        string? GetLayer(Project project)
        {
            var root = Path.GetRelativePath(Solution.Directory, Path.GetFullPath(LayeringRoot, Solution.Directory));
            return LayeringByFolder
                ? LayeringTasks.GetFolderLayer(RelativePath(project), root)
                : LayeringTasks.GetDefaultLayer(project.Name, RelativePath(project), root);
        }

        Target CheckLayering => d => d
            .Description("Verifies the layered architecture: project references, file imports and layer folders")
            .Executes(() => {
                var projects = LayeringTasks.Collect(Solution, GetLayer);
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
