namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>
/// How the layering check determines a project's layer.
/// </summary>
public enum LayeringMode
{
    /// <summary>
    /// By folder: libraries live in <c>src/Library/&lt;Layer&gt;</c> and applications in <c>src/Apps/Apis</c>,
    /// <c>src/Apps/Jobs</c> and <c>src/Apps/Ui</c>, which map to the <c>Apps</c> layer.
    /// </summary>
    Folder,

    /// <summary>
    /// By name prefix: libraries live in <c>src/Library.&lt;Layer&gt;</c> and applications in
    /// <c>src/Apps.&lt;Kind&gt;.&lt;Name&gt;</c>.
    /// </summary>
    Prefix
}
