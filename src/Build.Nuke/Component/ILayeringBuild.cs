using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Serilog;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    /// <summary>
    /// Enforces the solution's layered architecture, where a project's layer is the prefix of its name
    /// (<c>src/&lt;Layer&gt;.&lt;Name&gt;/</c>): project references, source file imports and layer folders may only point
    /// to the project's own or a lower layer. The <c>Testing</c> kits may reference anything, but nothing in a layer may
    /// reference them. See <see cref="LayeringTasks.Check"/> for the exact rules.
    /// </summary>
    public interface ILayeringBuild : IStandardBuild
    {
        /// <summary>The layered chain, innermost first. By default the toolkit's one (see the architecture guide).</summary>
        IReadOnlyList<string> LayeringLayers => LayeringTasks.DefaultLayers;

        /// <summary>
        /// The layer of a project, or <c>null</c> to leave it out of the check. By default the prefix of its name for
        /// projects under <c>src/</c>; a prefix that is neither one of the <see cref="LayeringLayers"/> nor <c>Testing</c>
        /// is a violation.
        /// </summary>
        string? GetLayer(Project project) => LayeringTasks.GetDefaultLayer(project.Name, RelativePath(project));

        Target CheckLayering => d => d
            .Description("Verifies the layered architecture: project references, file imports and layer folders")
            .Executes(() => {
                var projects = LayeringTasks.Collect(Solution, GetLayer);
                var violations = LayeringTasks.Check(projects, LayeringLayers);

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
