using Anarchy.Toolkit.Automation.Build.Extensions.Fallout.Solutions;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Fallout.Common;
using Fallout.Solutions;
using Serilog;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Enforces the solution's layered architecture (sometimes called an onion architecture), using optional <c>.anarchy-toolkit.json</c> overrides: references, imports
/// and folders may only point to the project's own or a lower layer. The <c>Testing</c> kits may reference anything,
/// but nothing in a layer may reference them. See <see cref="LayeringTasks"/> for the exact rules and
/// <see cref="LayeringConfiguration"/> for the file.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IStandardBuild.PullRequest"/> runs <see cref="CheckLayering"/> when the build implements this interface.
/// </para>
/// <para>
/// Missing preferences use the in-code folder-mode defaults; the layout is never inferred from project names.
/// </para>
/// </remarks>
/// <example>
/// <code language="json">
/// {
///   "layering": { "mode": "folder",
///   "layers": ["Core", "Infrastructure", "Bootstrap", "Runtime", "Apps"],
///   "libraryRoot": "src/Library",
///   "applicationFolders": { "src/Apps/Apis": "Apps", "src/Apps/Jobs": "Apps", "src/Apps/Ui": "Apps" } }
/// }
/// </code>
/// </example>
public interface ILayeringBuild : IStandardBuild
{
    /// <summary>
    /// The effective layout, resolving invocation arguments over saved overrides and in-code defaults.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when supplied overrides produce an invalid layout.</exception>
    SolutionLayout Layering => ResolveLayering();

    /// <summary>
    /// The layer of a project, or <c>null</c> to leave it out of the check. By default, the layer the
    /// <paramref name="layout"/> gives it (see <see cref="LayeringTasks.GetLayer(string, string, SolutionLayout)"/>).
    /// An unknown layer is a violation.
    /// </summary>
    /// <param name="project">The solution project.</param>
    /// <param name="layout">The solution's layout (see <see cref="Layering"/>).</param>
    /// <returns>The project's layer, or <see langword="null"/> to leave it out.</returns>
    string? GetLayer(Project project, SolutionLayout layout) =>
        LayeringTasks.GetLayer(project.Name, RelativePath(project), layout);

    /// <summary>
    /// Checks project references, file imports and layer folders against the configured layers, logging every
    /// violation and failing the build when there is any.
    /// </summary>
    Target CheckLayering => d => d
        .Description("Verifies the layered architecture: project references, file imports and layer folders")
        .Executes(() => {
            var layout = Layering;
            var solutionProjects = Solution.ToProjectInfos(RelativePath);
            var projects = LayeringTasks.Collect(solutionProjects.Keys, info => GetLayer(solutionProjects[info], layout));
            var violations = LayeringTasks.Check(projects, layout);

            if (violations.Count > 0)
            {
                foreach (var violation in violations)
                {
                    Log.Error("{Violation}", violation);
                }

                Assert.Fail($"{violations.Count} layering violations. To opt out, remove ILayeringBuild from build/Build.cs; to adapt your layout, use layering preferences. See https://anarchy-io.github.io/anarchy-toolkit/conventions/layering.html#opting-out-or-adapting.");
            }

            Log.Information("Layering OK across {Projects} projects by {Mode}: {Layers} (projects, files and folders)",
                projects.Count, layout.Mode == LayeringMode.Folder ? "folder" : "name prefix", string.Join(" < ", layout.Layers));
        });
}
