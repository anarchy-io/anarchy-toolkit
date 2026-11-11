using Fallout.Common.IO;

namespace Anarchy.Toolkit.Build.Fallout.Core.Entity.Build
{
    /// <summary>
    /// Represents a deployable application.
    /// </summary>
    public sealed partial class Artifact
    {
        /// <summary>
        /// Project file to build.
        /// </summary>
        public AbsolutePath? ProjectFile { get; set; }

        /// <summary>
        /// The name of the artifact, as derived from the project file path.
        /// </summary>
        public string? ProjectName => ProjectFile != null ? Path.GetFileName(ProjectFile.Parent.ToString()) : null;
    }
}
