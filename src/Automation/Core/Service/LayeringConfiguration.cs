using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Validation;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>Resolves and validates the optional layering overrides in the solution-root preferences file.</summary>
public static class LayeringConfiguration
{
    /// <summary>The unified preferences filename.</summary>
    public const string FileName = AutomationPreferences.FileName;
    /// <summary>The unified preferences schema.</summary>
    public const string SchemaUrl = AutomationPreferences.SchemaUrl;
    /// <summary>Returns the preferences path under the solution root.</summary>
    public static string PathOf(string root) => Path.Combine(root, FileName);
    /// <summary>Resolves saved overrides over the in-code folder-mode defaults.</summary>
    public static SolutionLayout Read(string root) => AutomationPreferences.Read(root).ResolveLayout();
    /// <summary>Resolves a unified preferences document.</summary>
    public static SolutionLayout Parse(string json, string source) => AutomationPreferences.Parse(json, source).ResolveLayout();

    /// <summary>Validates an effective layout, including path bounds and unambiguous folder mappings.</summary>
    public static IReadOnlyList<string> Validate(SolutionLayout layout)
    {
        var errors = new List<string>();
        if (NameRules.ValidateLayers(layout.Layers) is { } layers) errors.Add($"layers: {layers}");
        var root = CheckPath("root", layout.Root, errors);
        if (layout.Mode == LayeringMode.Prefix)
        {
            if (layout.LibraryRoot is not null || layout.ApplicationFolders.Count > 0)
                errors.Add("prefix mode cannot have a library root or application folders: a solution uses one layering mode");
            return errors;
        }
        var library = layout.LibraryRoot is null ? null : CheckPath("libraryRoot", layout.LibraryRoot, errors);
        if (library is null) errors.Add("folder mode needs an effective libraryRoot");
        if (library is not null && root is not null && !LayeringTasks.IsWithin(library, root))
            errors.Add($"libraryRoot {library} is outside root {root}");
        var folders = new List<(string Path, string Layer)>();
        foreach (var (folder, layer) in layout.ApplicationFolders)
        {
            if (CheckPath($"applicationFolders {folder}", folder, errors) is not { } path) continue;
            if (!layout.Layers.Contains(layer, StringComparer.Ordinal))
                errors.Add($"application folder {path} is mapped to {layer}, which is not one of the layers ({string.Join(", ", layout.Layers)})");
            if (root is not null && !LayeringTasks.IsWithin(path, root)) errors.Add($"application folder {path} is outside root {root}");
            if (library is not null && LayeringTasks.IsWithin(library, path))
                errors.Add($"application folder {path} contains the library root {library}, so the library layers would be ambiguous");
            if (library is not null && Parent(path) is { } parent && string.Equals(parent, library, StringComparison.OrdinalIgnoreCase) &&
                Name(path) is var name && (layout.Layers.Contains(name, StringComparer.OrdinalIgnoreCase) || name == LayeringTasks.Testing) &&
                !string.Equals(name, layer, StringComparison.Ordinal))
                errors.Add($"application folder {path} (mapped to {layer}) is also the {name} layer folder of {library}, so its layer is ambiguous");
            foreach (var other in folders.Where(other => LayeringTasks.IsWithin(path, other.Path) || LayeringTasks.IsWithin(other.Path, path)))
                errors.Add(string.Equals(other.Path, path, StringComparison.OrdinalIgnoreCase)
                    ? $"application folder {path} is listed more than once"
                    : $"application folders {other.Path} and {path} overlap, so the layer of the projects in both is ambiguous");
            folders.Add((path, layer));
        }
        return errors;
    }

    /// <summary>Serializes an explicit layout as a unified preferences document.</summary>
    public static string ToJson(SolutionLayout layout) => new JsonObject {
        ["$schema"] = SchemaUrl,
        ["layering"] = AutomationPreferences.LayoutOverrides(layout)
    }.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";

    /// <summary>Explicitly writes a new preferences file, refusing to overwrite an existing one.</summary>
    public static void Write(string root, SolutionLayout layout)
    {
        if (Validate(layout) is { Count: > 0 } errors)
            throw new InvalidOperationException($"{FileName}: layering: {string.Join("; ", errors)}");
        var content = ToJson(layout);
        AutomationPreferences.Parse(content);
        Directory.CreateDirectory(root);
        using var stream = new FileStream(PathOf(root), FileMode.CreateNew);
        using var writer = new StreamWriter(stream);
        writer.Write(content);
    }

    private static string? CheckPath(string name, string path, List<string> errors)
    {
        var normalized = LayeringTasks.NormalizePath(path);
        // Reject traversal rather than relying on lexical prefix checks (src/../../outside starts with src/).
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(normalized) || normalized.Split('/').Contains("..") || normalized.Contains(':'))
        {
            errors.Add($"{name} \"{path}\" must be a folder inside the solution, relative to its root");
            return null;
        }
        return normalized;
    }
    private static string? Parent(string path) => path.Contains('/') ? path[..path.LastIndexOf('/')] : path == "." ? null : ".";
    private static string Name(string path) => path.Split('/')[^1];
}
