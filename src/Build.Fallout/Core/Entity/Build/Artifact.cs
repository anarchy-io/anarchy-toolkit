using Fallout.Common.IO;

namespace Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;

/// <summary>
/// Represents a deployable application.
/// </summary>
public sealed class Artifact
{
    /// <summary>
    /// The project file to build.
    /// </summary>
    public AbsolutePath? ProjectFile { get; set; }

    /// <summary>
    /// The name of the artifact: the name of the directory holding <see cref="ProjectFile"/>, or <c>null</c> when
    /// no project file is set.
    /// </summary>
    public string? ProjectName => ProjectFile != null ? Path.GetFileName(ProjectFile.Parent.ToString()) : null;
}
