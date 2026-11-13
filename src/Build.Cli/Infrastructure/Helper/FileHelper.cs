namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>File operations that never replace existing content.</summary>
public static class FileHelper
{
    /// <summary>Writes a new file, creating its directory; fails if the file already exists.</summary>
    public static void WriteNewText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = new StreamWriter(new FileStream(path, FileMode.CreateNew));
        output.Write(content);
    }

    /// <summary>The existing entry of a directory matching a name case-insensitively, or the path it would have.</summary>
    public static string ExistingEntryPath(string directory, string name) =>
        Directory.EnumerateFileSystemEntries(directory)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), name, StringComparison.OrdinalIgnoreCase))
        ?? Path.Combine(directory, name);

    /// <summary>Whether a directory is missing or has no entries; false for a file.</summary>
    public static bool IsMissingOrEmptyDirectory(string path) =>
        !File.Exists(path) && (!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any());

    /// <summary>Appends content to a file, separated by a blank line, unless the file already contains it.</summary>
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
}
