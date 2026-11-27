namespace Anarchy.Toolkit.Automation.Helpers.Solutions;

/// <summary>
/// Lists solution-level files in a <c>.sln</c> under the <see cref="FolderName"/> solution folder, mirroring their
/// directories as nested solution folders (e.g. <c>scripts/make/help.sh</c> under <c>solution items/scripts/make</c>).
/// </summary>
public static class SolutionItems
{
    /// <summary>
    /// The name of the solution folder holding the solution items.
    /// </summary>
    public const string FolderName = "solution items";

    private const string FolderType = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

    /// <summary>
    /// Adds the existing files and the files of the existing directories among <paramref name="paths"/> to the
    /// solution's <see cref="FolderName"/> folder, creating the folders it needs; missing paths and files already
    /// listed are skipped.
    /// </summary>
    /// <param name="root">The solution directory.</param>
    /// <param name="solutionFile">The solution file, relative to <paramref name="root"/>.</param>
    /// <param name="paths">The files and directories to add, relative to <paramref name="root"/>.</param>
    public static void Add(string root, string solutionFile, IEnumerable<string> paths)
    {
        var solutionPath = Path.Combine(root, solutionFile);
        var text = File.ReadAllText(solutionPath);
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();

        foreach (var file in paths.SelectMany(path => Expand(root, path)).Distinct(StringComparer.Ordinal))
        {
            var separator = file.LastIndexOf('/');
            var folder = EnsureFolder(lines, separator < 0 ? string.Empty : file[..separator]);
            AddFile(lines, folder, file.Replace('/', '\\'));
        }

        File.WriteAllText(solutionPath, string.Join(newline, lines));
    }

    private static IEnumerable<string> Expand(string root, string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/');
        var full = Path.Combine(root, normalized);
        if (File.Exists(full)) return [normalized];
        if (!Directory.Exists(full)) return [];
        return Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);
    }

    private static string EnsureFolder(List<string> lines, string directory)
    {
        string? parent = null;
        var name = FolderName;
        if (directory.Length > 0)
        {
            var separator = directory.LastIndexOf('/');
            parent = EnsureFolder(lines, separator < 0 ? string.Empty : directory[..separator]);
            name = directory[(separator + 1)..];
        }

        var nested = NestedParents(lines);
        var existing = lines
            .Select(ParseFolder)
            .FirstOrDefault(folder => folder is { } found && found.Name == name &&
                                      nested.GetValueOrDefault(found.Guid) == parent);
        if (existing is { } match) return match.Guid;

        var guid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}";
        var global = lines.FindIndex(line => line == "Global");
        lines.InsertRange(global < 0 ? lines.Count : global, [$"Project(\"{FolderType}\") = \"{name}\", \"{name}\", \"{guid}\"", "EndProject"]);
        if (parent is not null) AddNesting(lines, guid, parent);
        return guid;
    }

    private static (string Name, string Guid)? ParseFolder(string line)
    {
        var prefix = $"Project(\"{FolderType}\") = \"";
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var parts = line[prefix.Length..].Split('"');
        return parts.Length >= 5 ? (parts[0], parts[4]) : null;
    }

    private static Dictionary<string, string> NestedParents(List<string> lines)
    {
        var start = lines.FindIndex(line => line.Trim() == "GlobalSection(NestedProjects) = preSolution");
        var parents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (start < 0) return parents;
        for (var index = start + 1; index < lines.Count && lines[index].Trim() != "EndGlobalSection"; index++)
        {
            var pair = lines[index].Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 2) parents[pair[0]] = pair[1];
        }
        return parents;
    }

    private static void AddNesting(List<string> lines, string child, string parent)
    {
        var start = lines.FindIndex(line => line.Trim() == "GlobalSection(NestedProjects) = preSolution");
        if (start < 0)
        {
            var end = lines.FindLastIndex(line => line == "EndGlobal");
            start = end < 0 ? lines.Count : end;
            lines.InsertRange(start, ["\tGlobalSection(NestedProjects) = preSolution", "\tEndGlobalSection"]);
        }
        var close = lines.FindIndex(start, line => line.Trim() == "EndGlobalSection");
        lines.Insert(close, $"\t\t{child} = {parent}");
    }

    private static void AddFile(List<string> lines, string folder, string file)
    {
        var project = lines.FindIndex(line => ParseFolder(line) is { } found && found.Guid == folder);
        var end = lines.FindIndex(project, line => line == "EndProject");
        var section = lines.FindIndex(project, end - project, line => line.Trim() == "ProjectSection(SolutionItems) = preProject");
        if (section < 0)
        {
            lines.InsertRange(project + 1, ["\tProjectSection(SolutionItems) = preProject", "\tEndProjectSection"]);
            section = project + 1;
        }
        var close = lines.FindIndex(section, line => line.Trim() == "EndProjectSection");
        var entry = $"\t\t{file} = {file}";
        if (lines.GetRange(section, close - section).Contains(entry)) return;
        lines.Insert(close, entry);
    }
}
