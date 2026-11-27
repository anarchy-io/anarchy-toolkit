using Anarchy.Toolkit.Automation.Core.Entity;
using Fallout.Solutions;

namespace Anarchy.Toolkit.Automation.Build.Extensions.Fallout.Solutions;

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
    /// Maps the solution's projects to the plain inputs of the Automation checks, keyed back to their project.
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
                CodeProjectReferences(project)),
            project => project,
            ReferenceEqualityComparer.Instance);

    // A ProjectReference with ReferenceOutputAssembly="false" only orders the build (e.g. to embed a package's version):
    // it is no code dependency, so the layering check and the dependency map ignore it.
    private static string[] CodeProjectReferences(Project project) =>
        project.GetMSBuildProject().GetItems("ProjectReference")
            .Where(item => !string.Equals(item.GetMetadataValue("ReferenceOutputAssembly"), "false", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.EvaluatedInclude)
            .ToArray();
}
