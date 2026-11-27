using Anarchy.Toolkit.Automation.Helpers.Files;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Reads the packaged boilerplate files without installing them, and compares them with a solution's copies, so a
/// developer or an agent can see what <see cref="SolutionScaffolder"/> would write before it writes anything.
/// </summary>
/// <example>
/// <code language="csharp">
/// foreach (var file in BoilerplateCatalog.Compare(root, ["Makefile", "scripts"]))
///     Console.WriteLine($"{file.Path}: {file.Status}");
/// </code>
/// </example>
public static class BoilerplateCatalog
{
    private const string ResourcePrefix = "Anarchy.Boilerplate/";

    /// <summary>
    /// How a solution's copy of a packaged file compares with the packaged one.
    /// </summary>
    public enum FileStatus
    {
        /// <summary>The solution has no such file.</summary>
        Missing,

        /// <summary>The solution's file matches the packaged one, or already holds its managed block.</summary>
        Identical,

        /// <summary>The solution's file differs; see <see cref="FileComparison.Diff"/>.</summary>
        Different
    }

    /// <summary>
    /// The comparison of one packaged file.
    /// </summary>
    /// <param name="Path">The file's path relative to the solution root, with <c>/</c> separators.</param>
    /// <param name="Status">How the solution's copy compares.</param>
    /// <param name="Diff">A unified diff from the solution's copy to the packaged one; empty unless <see cref="FileStatus.Different"/>.</param>
    public sealed record FileComparison(string Path, FileStatus Status, string Diff);

    /// <summary>
    /// The paths of every packaged file, relative to the solution root, in ordinal order.
    /// </summary>
    /// <returns>The packaged paths, such as <c>Makefile</c> or <c>scripts/make/targets/agents.mk</c>.</returns>
    public static IReadOnlyList<string> List() => Resources().Select(resource => resource.Path).Order(StringComparer.Ordinal).ToArray();

    /// <summary>
    /// The text of a packaged file.
    /// </summary>
    /// <param name="path">A path returned by <see cref="List"/>; <c>\</c> separators are accepted.</param>
    /// <returns>The file's text, or <see langword="null"/> when no such file is packaged.</returns>
    public static string? Read(string path)
    {
        var normalized = Normalize(path);
        var resource = Resources().FirstOrDefault(resource => string.Equals(resource.Path, normalized, StringComparison.OrdinalIgnoreCase));
        if (resource.Name is null) return null;
        using var input = typeof(BoilerplateCatalog).Assembly.GetManifestResourceStream(resource.Name)!;
        using var reader = new StreamReader(input);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Compares the packaged files with the solution's copies. <c>.gitignore</c> and <c>.editorconfig</c> are compared
    /// with what syncing their managed block would write (see <see cref="FileHelper.MergeManagedBlock"/>),
    /// so the repository's own entries outside the block never count as differences.
    /// </summary>
    /// <param name="root">The solution root.</param>
    /// <param name="paths">
    /// Packaged paths or folders to compare (<c>scripts</c> covers every script); <see langword="null"/> or empty for
    /// every packaged file except the opt-in agent skills.
    /// </param>
    /// <returns>One comparison per selected packaged file, in path order.</returns>
    /// <exception cref="ArgumentException">Thrown when a path matches no packaged file.</exception>
    public static IReadOnlyList<FileComparison> Compare(string root, IReadOnlyCollection<string>? paths = null)
    {
        var all = List();
        var selected = paths is { Count: > 0 }
            ? paths.Select(Normalize).SelectMany(path => Select(all, path)).Distinct().ToArray()
            : all.Where(path => !path.StartsWith(".agents/", StringComparison.Ordinal)).ToArray();

        return selected.Select(path => {
            var packaged = Read(path)!;
            var local = FileHelper.ExistingEntryPath(Path.GetDirectoryName(Path.Combine(root, path))!, Path.GetFileName(path));
            if (!File.Exists(local)) return new FileComparison(path, FileStatus.Missing, "");
            var existing = File.ReadAllText(local);
            var expected = path is ".gitignore" or ".editorconfig"
                ? FileHelper.MergeManagedBlock(existing, packaged,
                    skipExistingLines: path == ".gitignore", placeBlockFirst: path is ".gitignore" or ".editorconfig")
                : packaged;
            return Normalized(existing) == Normalized(expected)
                ? new FileComparison(path, FileStatus.Identical, "")
                : new FileComparison(path, FileStatus.Different, UnifiedDiff.Create(path, existing, expected));
        }).ToArray();

        static string Normalized(string text) => text.ReplaceLineEndings("\n");
    }

    private static IEnumerable<string> Select(IReadOnlyList<string> all, string path)
    {
        var matches = all.Where(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase) ||
                                        item.StartsWith(path.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length > 0 ? matches : throw new ArgumentException($"No packaged boilerplate file '{path}'.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/').Trim().TrimStart('/');

    internal static IEnumerable<(string Name, string Path)> Resources() =>
        typeof(BoilerplateCatalog).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(name => (name, name[ResourcePrefix.Length..].Replace('\\', '/')));
}
