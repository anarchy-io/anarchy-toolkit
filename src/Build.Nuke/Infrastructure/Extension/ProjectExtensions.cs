using Nuke.Common.ProjectModel;

namespace Anarchy.Toolkit.Build.Nuke.Infrastructure.Extension
{
    /// <summary>
    /// Extensions to NUKE's <see cref="Project"/> type.
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
    }
}
