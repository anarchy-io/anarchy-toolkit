using Anarchy.Toolkit.Automation.Build.Infrastructure.Extension;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;
using Serilog;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Generates a static HTML page mapping the solution's projects by layer: a Mermaid dependency graph and an onion
/// of the layered chain whose rings zoom in on the references between a layer's own projects. Meant for CI to keep
/// the documentation up to date; the output only changes when the graph does.
/// </summary>
public interface IDependencyMapBuild : IStandardBuild
{
    /// <summary>
    /// Where <see cref="GenerateDependencyMap"/> writes the HTML page (<c>--dependency-map-output</c>); defaults to
    /// <c>docs/dependency-map.html</c>.
    /// </summary>
    [Parameter("Where GenerateDependencyMap writes the HTML page; defaults to docs/dependency-map.html")]
    AbsolutePath DependencyMapOutput => TryGetValue(() => DependencyMapOutput) ?? PreferencePath("documentation.dependencyMapOutput") ?? RootDirectory / "docs" / "dependency-map.html";

    /// <summary>
    /// The group a project is shown in, or <c>null</c> to leave it out. By default its layer in the
    /// <paramref name="layout"/> (see <c>.anarchy-toolkit.json</c>) when that is one of its layers or <c>Testing</c>;
    /// otherwise <c>Tests</c>, <c>Samples</c> or <c>Other</c> by location. The build project itself is left out.
    /// </summary>
    /// <param name="project">The solution project to group.</param>
    /// <param name="layout">The effective solution layout.</param>
    /// <returns>The group name, or <see langword="null"/> to leave the project off the map.</returns>
    string? GetDependencyMapGroup(Project project, SolutionLayout layout) =>
        DependencyMapTasks.GetDefaultGroup(project.Name, RelativePath(project), layout);

    /// <summary>
    /// Writes the dependency map of the solution to <see cref="DependencyMapOutput"/>, warning (without failing) about
    /// references that break the layering; they are highlighted on the map. The layers and each project's layer come
    /// from invocation arguments, optional <c>.anarchy-toolkit.json</c> overrides, and in-code defaults.
    /// </summary>
    Target GenerateDependencyMap => d => d
        .Description("Generates an HTML map of the solution's project dependencies by layer")
        .Executes(() => {
            var layout = this.As<IAutomationPreferencesBuild>().ResolveLayering();
            var solutionProjects = Solution.ToProjectInfos(RelativePath);
            var map = DependencyMapTasks.Collect(
                Solution.Name ?? "Solution",
                solutionProjects.Keys,
                info => GetDependencyMapGroup(solutionProjects[info], layout),
                layout.Layers);

            DependencyMapOutput.WriteAllText(DependencyMapTasks.RenderHtml(map));

            var violations = map.References.Count(map.IsViolation);
            Log.Information("Mapped {Projects} projects and {References} references into {Output}",
                map.Projects.Count, map.References.Count, DependencyMapOutput);

            if (violations > 0)
            {
                Log.Warning("{Violations} references break the layering; they are highlighted on the map", violations);
            }
        });
}
