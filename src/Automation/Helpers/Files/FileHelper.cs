using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Helpers.Files;

/// <summary>
/// File operations that never replace existing content.
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Writes a new file, creating its directory.
    /// </summary>
    /// <exception cref="System.IO.IOException">Thrown when the file already exists.</exception>
    /// <param name="path">The file path.</param>
    /// <param name="content">The text to write.</param>
    public static void WriteNewText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = new StreamWriter(new FileStream(path, FileMode.CreateNew));
        output.Write(content);
    }

    /// <summary>
    /// The existing entry of a directory matching a name case-insensitively, or the path it would have.
    /// </summary>
    /// <param name="directory">The directory, which must exist.</param>
    /// <param name="name">The file or folder name.</param>
    /// <returns>The existing entry's path, or <paramref name="name"/> combined with <paramref name="directory"/>.</returns>
    public static string ExistingEntryPath(string directory, string name) =>
        Directory.EnumerateFileSystemEntries(directory)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), name, StringComparison.OrdinalIgnoreCase))
        ?? Path.Combine(directory, name);

    /// <summary>
    /// Whether a directory is missing or has no entries; false for a file.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> does not exist or is an empty directory.</returns>
    public static bool IsMissingOrEmptyDirectory(string path) =>
        !File.Exists(path) && (!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any());

    /// <summary>
    /// The first line of a block of packaged settings that the boilerplate keeps in sync; see <see cref="MergeManagedBlock"/>.
    /// </summary>
    public const string ManagedBlockBegin = "# >>> anarchy-toolkit";

    /// <summary>
    /// The last line of a block of packaged settings that the boilerplate keeps in sync.
    /// </summary>
    public const string ManagedBlockEnd = "# <<< anarchy-toolkit";

    /// <summary>
    /// Merges packaged settings into an existing settings file. The packaged text is wrapped in a managed block (lines
    /// starting with <see cref="ManagedBlockBegin"/> and <see cref="ManagedBlockEnd"/>): the file's own block is
    /// replaced, or a new one is appended (or placed first for <c>.gitignore</c> and <c>.editorconfig</c>), and
    /// everything outside it is kept. With <paramref name="skipExistingLines"/>, setting lines (neither blank nor
    /// comments) that the file already has outside the block are left out of it. With <paramref name="placeBlockFirst"/>,
    /// the managed block is placed before existing settings. For <c>.editorconfig</c>, an existing global <c>root</c>
    /// directive is removed so the packaged <c>root = true</c> remains before every section.
    /// </summary>
    /// <param name="existing">The current content of the file.</param>
    /// <param name="packaged">The packaged content, with or without its own block markers.</param>
    /// <param name="skipExistingLines">Whether to leave out lines the file already has, as for <c>.gitignore</c>.</param>
    /// <param name="placeBlockFirst">Whether to put the packaged block before existing settings, as for the repository settings files.</param>
    /// <returns>The merged content, equal to <paramref name="existing"/> when the file is up to date.</returns>
    public static string MergeManagedBlock(string existing, string packaged, bool skipExistingLines, bool placeBlockFirst = false)
    {
        var newline = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var block = SplitLines(packaged);
        var begin = block.FindIndex(IsBlockBegin);
        var end = block.FindIndex(IsBlockEnd);
        var header = begin >= 0 ? block[begin] : ManagedBlockBegin + ": managed by 'make boilerplate'; edit outside the block >>>";
        var footer = end >= 0 ? block[end] : ManagedBlockEnd + " <<<";
        var body = begin >= 0 && end > begin ? block[(begin + 1)..end] : block;

        var lines = SplitLines(existing);
        var start = lines.FindIndex(IsBlockBegin);
        var stop = start >= 0 ? lines.FindIndex(start, IsBlockEnd) : -1;
        var before = start >= 0 && stop > start ? lines[..start] : lines;
        var after = start >= 0 && stop > start ? lines[(stop + 1)..] : [];

        if (skipExistingLines)
        {
            var own = before.Concat(after).Select(line => line.Trim()).Where(IsSetting).ToHashSet(StringComparer.Ordinal);
            body = body.Where(line => !IsSetting(line.Trim()) || !own.Contains(line.Trim())).ToList();
            body = RemoveEmptyGitIgnoreHeadings(body);
        }

        var merged = placeBlockFirst ? new List<string>() : new List<string>(before);
        if (!placeBlockFirst && (start < 0 || stop <= start))
        {
            while (merged.Count > 0 && merged[^1].Length == 0) merged.RemoveAt(merged.Count - 1);
            if (merged.Count > 0) merged.Add("");
        }
        merged.Add(header);
        merged.AddRange(body);
        merged.Add(footer);

        if (placeBlockFirst)
        {
            var outside = before.Concat(after);
            var preserved = (skipExistingLines ? outside : outside.Where(line => !IsEditorConfigRootDirective(line))).ToList();
            while (preserved.Count > 0 && preserved[0].Length == 0) preserved.RemoveAt(0);
            if (preserved.Any(line => line.Length > 0)) merged.Add("");
            merged.AddRange(preserved);
        }
        else
        {
            merged.AddRange(after);
        }

        var result = string.Join(newline, merged) + newline;
        return SplitLines(result).SequenceEqual(lines) ? existing : result;

        static bool IsSetting(string line) => line.Length > 0 && !line.StartsWith('#') && !line.StartsWith(';');
    }

    private static List<string> RemoveEmptyGitIgnoreHeadings(List<string> lines)
    {
        var remove = new HashSet<int>();
        for (var index = 0; index < lines.Count; index++)
        {
            if (!IsGitIgnoreHeading(lines[index])) continue;

            var nextHeading = index + 1;
            while (nextHeading < lines.Count && !IsGitIgnoreHeading(lines[nextHeading])) nextHeading++;
            var contents = lines.Skip(index + 1).Take(nextHeading - index - 1).ToArray();
            var hasSetting = contents.Any(line => IsSetting(line.Trim()));
            var hasSupportingComment = contents.Any(line => {
                var trimmed = line.Trim();
                return trimmed.StartsWith("# ", StringComparison.Ordinal) && !IsGitIgnoreHeading(trimmed);
            });
            if (!hasSetting && !hasSupportingComment)
                remove.Add(index);
        }

        return lines.Where((_, index) => !remove.Contains(index)).ToList();

        static bool IsSetting(string line) => line.Length > 0 && !line.StartsWith('#') && !line.StartsWith(';');
    }

    private static bool IsGitIgnoreHeading(string line)
    {
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("# ", StringComparison.Ordinal) || trimmed.StartsWith("##", StringComparison.Ordinal))
            return false;

        var text = trimmed[2..].Trim();
        return text.Length > 0 && !text.StartsWith("Note:", StringComparison.OrdinalIgnoreCase) &&
               !text.StartsWith("Uncomment", StringComparison.OrdinalIgnoreCase) &&
               !text.StartsWith("but ", StringComparison.OrdinalIgnoreCase) &&
               !text.StartsWith("because ", StringComparison.OrdinalIgnoreCase) &&
               !text.StartsWith('(') && !char.IsLower(text[0]) && !text.EndsWith('.') && !text.EndsWith(':') &&
               !text.EndsWith(',');
    }

    private static bool IsEditorConfigRootDirective(string line)
    {
        var trimmed = line.Trim();
        var separator = trimmed.IndexOf('=');
        return separator >= 0 && string.Equals(trimmed[..separator].Trim(), "root", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlockBegin(string line) => line.StartsWith(ManagedBlockBegin, StringComparison.Ordinal);

    private static bool IsBlockEnd(string line) => line.StartsWith(ManagedBlockEnd, StringComparison.Ordinal);

    private static List<string> SplitLines(string text)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        if (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    /// <summary>
    /// Appends content to a file, separated by a blank line, unless the file already contains it.
    /// </summary>
    /// <param name="destination">The existing file to append to.</param>
    /// <param name="input">The content to append, read to its end.</param>
    public static void AppendOnce(string destination, Stream input)
    {
        using var memory = new MemoryStream();
        input.CopyTo(memory);
        var addition = memory.ToArray();
        var existing = File.ReadAllBytes(destination);
        if (existing.AsSpan().IndexOf(addition) >= 0) return;

        using var output = new FileStream(destination, FileMode.Append, FileAccess.Write);
        if (existing.Length > 0 && existing[^1] != (byte)'\n') output.WriteByte((byte)'\n');
        if (existing.Length > 0) output.WriteByte((byte)'\n');
        output.Write(addition);
    }

    /// <summary>
    /// Adds a top-level property of the JSON object in <paramref name="input"/> to the JSON object in a file,
    /// unless the file already has it (compared case-insensitively). Other content is kept, but the file is
    /// rewritten indented.
    /// </summary>
    /// <param name="destination">The existing JSON file.</param>
    /// <param name="input">The JSON object holding the property.</param>
    /// <param name="propertyName">The property to add.</param>
    /// <returns>
    /// Whether the property was added; false when the file already has it, the input lacks it, or the file is not
    /// a plain JSON object (one with comments, for example, is left alone rather than lose them).
    /// </returns>
    public static bool AddMissingJsonProperty(string destination, Stream input, string propertyName)
    {
        if (JsonNode.Parse(input) is not JsonObject packaged ||
            !packaged.TryGetPropertyValue(propertyName, out var addition))
            return false;

        JsonObject existing;
        try
        {
            if (JsonNode.Parse(File.ReadAllText(destination)) is not JsonObject parsed) return false;
            existing = parsed;
        }
        catch (JsonException)
        {
            return false;
        }

        if (existing.Any(property => string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase)))
            return false;

        existing[propertyName] = addition?.DeepClone();
        File.WriteAllText(destination, existing.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        return true;
    }
}
