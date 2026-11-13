namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>Controls which scaffold items are installed and whether existing text settings are appended.</summary>
public sealed class ScaffoldOptions
{
    /// <summary>Comma-separated item names or script paths; null means all items.</summary>
    public string? Items { get; set; }

    /// <summary>Comma-separated item names or script paths to remove from the selection.</summary>
    public string? Exclude { get; set; }

    /// <summary>Append the packaged text to existing .gitignore and .editorconfig files.</summary>
    public bool AppendExisting { get; set; }

    /// <summary>Receives progress messages; null discards them.</summary>
    public Action<string>? Log { get; set; }
}
