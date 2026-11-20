namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>
/// Controls which scaffold items are installed and whether existing text settings are appended.
/// </summary>
public sealed class ScaffoldOptions
{
    /// <summary>
    /// Comma-separated item names or script paths; null means all items except the opt-in agent skills,
    /// which only <c>skills</c> selects.
    /// </summary>
    public string? Items { get; set; }

    /// <summary>
    /// Comma-separated item names or script paths to remove from the selection.
    /// </summary>
    public string? Exclude { get; set; }

    /// <summary>
    /// Sync the packaged text into a managed block of existing .gitignore and .editorconfig files, and add the packaged test runner to
    /// an existing global.json that does not choose one.
    /// </summary>
    public bool AppendExisting { get; set; }

    /// <summary>
    /// Only report, through <see cref="Log"/>, what would be created, kept, updated or conflict, without writing.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Ordered, solution-relative agent-skill destinations. Skills are created in the first directory. Null uses the
    /// default destination; an explicitly empty list is invalid when skills are selected.
    /// </summary>
    public IReadOnlyList<string>? SkillDirectories { get; set; }

    /// <summary>
    /// Receives progress messages; null discards them.
    /// </summary>
    public Action<string>? Log { get; set; }
}
