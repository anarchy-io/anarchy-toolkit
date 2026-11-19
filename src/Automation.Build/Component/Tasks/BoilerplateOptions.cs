namespace Anarchy.Toolkit.Automation.Build.Component.Tasks;

/// <summary>
/// Controls which scaffold items are installed and whether existing settings files are appended to.
/// </summary>
/// <remarks>
/// Passed to <see cref="BoilerplateTasks.Create(string, string?, BoilerplateOptions)"/>, which maps it onto the
/// <c>ScaffoldOptions</c> of <c>Anarchy.Toolkit.Automation</c>. <see cref="IBoilerplateBuild"/> fills it from its parameters.
/// </remarks>
public sealed class BoilerplateOptions
{
    /// <summary>
    /// Comma-separated item names or script paths; <c>null</c> means all items except the opt-in agent skills,
    /// which only <c>skills</c> selects.
    /// </summary>
    public string? Items { get; set; }
    /// <summary>
    /// Comma-separated item names or script paths to remove from the selection.
    /// </summary>
    public string? Exclude { get; set; }
    /// <summary>
    /// Whether to sync the packaged text into a managed block of existing <c>.gitignore</c> and <c>.editorconfig</c> files
    /// (see <c>FileHelper.MergeManagedBlock</c>), and add the packaged
    /// <c>test</c> runner section to an existing <c>global.json</c> that has none, instead of leaving them untouched.
    /// </summary>
    public bool AppendExisting { get; set; }
    /// <summary>
    /// Whether to only log what would be created, kept, updated or conflict, without writing anything.
    /// </summary>
    public bool DryRun { get; set; }
}
