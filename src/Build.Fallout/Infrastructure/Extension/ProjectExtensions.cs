using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Fallout.Solutions;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;

/// <summary>
/// Extensions to Fallout's <see cref="Project"/> type.
/// </summary>
public static class ProjectExtensions
{
    /// <summary>
    /// Returns whether the specified project contains a <c>PackageReference</c> for any of the specified packages.
    /// Comparison is case-insensitive.
    /// </summary>
    /// <param name="project">The project to inspect.</param>
    /// <param name="packages">The package IDs to look for.</param>
    /// <returns>
    /// <see langword="true"/> if the project references at least one of <paramref name="packages"/>; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public static bool ContainsPackageReferenceToAny(this Project project, IEnumerable<string> packages) =>
        project.GetItems<string>("PackageReference")
            .Any(r => packages.Contains(r, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Maps the solution's projects to the plain inputs of the Build.Cli checks, keyed back to their project.
    /// </summary>
    /// <remarks>
    /// A project's namespace is its <c>RootNamespace</c> property, falling back to its name. The dictionary compares
    /// keys by reference, so look up the very <see cref="SolutionProjectInfo"/> instances it returns.
    /// </remarks>
    /// <param name="solution">The solution whose projects (<see cref="Solution.AllProjects"/>) to map.</param>
    /// <param name="relativePath">Returns a project's path relative to the solution, e.g. <c>IStandardBuild.RelativePath</c>.</param>
    /// <returns>Each project's <see cref="SolutionProjectInfo"/>, mapped to the project it was created from.</returns>
    public static IReadOnlyDictionary<SolutionProjectInfo, Project> ToProjectInfos(this Solution solution, Func<Project, string> relativePath) =>
        solution.AllProjects.ToDictionary<Project, SolutionProjectInfo, Project>(
            project => new SolutionProjectInfo(
                project.Name,
                project.Path,
                project.Directory,
                relativePath(project),
                project.GetProperty("RootNamespace") ?? project.Name,
                project.GetItems<string>("ProjectReference").ToArray()),
            project => project,
            ReferenceEqualityComparer.Instance);
}
