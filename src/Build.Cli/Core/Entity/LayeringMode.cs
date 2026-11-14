namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// How the layering check determines a project's layer.
/// </summary>
public enum LayeringMode
{
    /// <summary>
    /// By folder: libraries live in <c>src/Library/&lt;Layer&gt;</c> and applications in <c>src/Api</c>, <c>src/Jobs</c>
    /// and <c>src/Ui</c>, which map to the <c>Web</c> and <c>Hosting</c> layers.
    /// </summary>
    Folder,

    /// <summary>
    /// By name prefix: every project lives in <c>src/&lt;Layer&gt;.&lt;Name&gt;</c>.
    /// </summary>
    Prefix
}
