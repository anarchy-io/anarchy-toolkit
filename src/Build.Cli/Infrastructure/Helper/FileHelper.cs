using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// File operations that never replace existing content.
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Writes a new file, creating its directory.
    /// </summary>
    /// <exception cref="IOException">Thrown when the file already exists.</exception>
    public static void WriteNewText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = new StreamWriter(new FileStream(path, FileMode.CreateNew));
        output.Write(content);
    }

    /// <summary>
    /// The existing entry of a directory matching a name case-insensitively, or the path it would have.
    /// </summary>
    public static string ExistingEntryPath(string directory, string name) =>
        Directory.EnumerateFileSystemEntries(directory)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), name, StringComparison.OrdinalIgnoreCase))
        ?? Path.Combine(directory, name);

    /// <summary>
    /// Whether a directory is missing or has no entries; false for a file.
    /// </summary>
    public static bool IsMissingOrEmptyDirectory(string path) =>
        !File.Exists(path) && (!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any());

    /// <summary>
    /// Appends content to a file, separated by a blank line, unless the file already contains it.
    /// </summary>
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
