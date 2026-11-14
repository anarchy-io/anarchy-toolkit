namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks;

/// <summary>
/// Controls which scaffold items are installed and whether existing settings files are appended to.
/// </summary>
public sealed class BoilerplateOptions
{
    /// <summary>
    /// Comma-separated item names or script paths; <c>null</c> means all items.
    /// </summary>
    public string? Items { get; set; }
    /// <summary>
    /// Comma-separated item names or script paths to remove from the selection.
    /// </summary>
    public string? Exclude { get; set; }
    /// <summary>
    /// Whether to append the packaged text to existing <c>.gitignore</c> and <c>.editorconfig</c> files, and add the packaged
    /// <c>test</c> runner section to an existing <c>global.json</c> that has none, instead of leaving them untouched.
    /// </summary>
    public bool AppendExisting { get; set; }
}
