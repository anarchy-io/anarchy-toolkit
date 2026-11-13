using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Fallout.Solutions;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension
{
    /// <summary>
    /// Extensions to Fallout's <see cref="Project"/> type.
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// Returns whether the specified project contains a <c>PackageReference</c> for any of the specified packages.
        /// Comparison is case-insensitive.
        /// </summary>
        public static bool ContainsPackageReferenceToAny(this Project project, IEnumerable<string> packages) =>
            project.GetItems<string>("PackageReference")
                   .Any(r => packages.Contains(r, StringComparer.OrdinalIgnoreCase));

        /// <summary>Maps the solution's projects to the plain inputs of the Build.Cli checks, keyed back to their project.</summary>
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
}
