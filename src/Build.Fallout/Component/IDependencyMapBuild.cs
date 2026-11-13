using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;
using Serilog;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    /// <summary>
    /// Generates a static HTML page mapping the solution's projects by layer: a Mermaid dependency graph and an onion
    /// of the layered chain whose rings zoom in on the references between a layer's own projects. Meant for CI to keep
    /// the documentation up to date; the output only changes when the graph does.
    /// </summary>
    public interface IDependencyMapBuild : IStandardBuild
    {
        [Parameter("Where GenerateDependencyMap writes the HTML page; defaults to docs/dependency-map.html")]
        AbsolutePath DependencyMapOutput => TryGetValue(() => DependencyMapOutput) ?? RootDirectory / "docs" / "dependency-map.html";

        /// <summary>The layered chain, innermost first. By default the toolkit's one (see the architecture guide).</summary>
        IReadOnlyList<string> DependencyMapLayers => LayeringTasks.DefaultLayers;

        /// <summary>
        /// The group a project is shown in, or <c>null</c> to leave it out. By default the prefix of its name when it
        /// is one of the <see cref="DependencyMapLayers"/> or <c>Testing</c>; otherwise <c>Tests</c>, <c>Samples</c>
        /// or <c>Other</c> by location. The build project itself is left out.
        /// </summary>
        string? GetDependencyMapGroup(Project project) =>
            DependencyMapTasks.GetDefaultGroup(project.Name, RelativePath(project), DependencyMapLayers);

        Target GenerateDependencyMap => d => d
            .Description("Generates an HTML map of the solution's project dependencies by layer")
            .Executes(() => {
                var solutionProjects = Solution.ToProjectInfos(RelativePath);
                var map = DependencyMapTasks.Collect(
                    Solution.Name ?? "Solution",
                    solutionProjects.Keys,
                    info => GetDependencyMapGroup(solutionProjects[info]),
                    DependencyMapLayers);

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
}
