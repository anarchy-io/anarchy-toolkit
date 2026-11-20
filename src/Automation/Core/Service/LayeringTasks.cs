using Anarchy.Toolkit.Automation.Core.Entity;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Checks that a solution follows its layered architecture (see <c>ILayeringBuild</c>): projects, source files
/// and root folders only depend on their own or a lower layer.
/// </summary>
public static partial class LayeringTasks
{
    /// <summary>
    /// The group of the test kits: they may reference any layer, but no layer may reference them.
    /// </summary>
    public const string Testing = "Testing";

    /// <summary>
    /// The default layered chain, innermost first (see the architecture guide), used when a solution's
    /// <c>.anarchy-toolkit.json</c> overrides no <c>layering.layers</c>. <c>Automation</c> (build tooling) sits on top so that no shipped
    /// library can depend on it.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultLayers = SolutionLayout.DefaultLayers;

    /// <summary>
    /// The layer of a project in a solution <paramref name="layout"/> (see <see cref="LayeringConfiguration"/>), or
    /// <see langword="null"/> for a project outside its <see cref="SolutionLayout.Root"/>, which is not checked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By <see cref="LayeringMode.Prefix"/>, the layer is the project name's prefix (up to the first dot). By
    /// <see cref="LayeringMode.Folder"/>, it is the layer of the application folder containing the project, else
    /// the first folder under the library root; the name is never used, so a prefix-like name is not a violation.
    /// </para>
    /// <para>
    /// A folder-mode project in neither the library root's layer folders nor an application folder gets an empty
    /// layer, which <see cref="Check(IReadOnlyCollection{LayeringProject}, SolutionLayout)"/> reports: the layout
    /// does not map it, and the other mode is never tried.
    /// </para>
    /// </remarks>
    /// <param name="projectName">The project name.</param>
    /// <param name="relativePath">The project file's path relative to the solution directory.</param>
    /// <param name="layout">The solution's layout.</param>
    /// <returns>The layer, an empty string when the layout does not map the project, or <see langword="null"/> when it is not checked.</returns>
    public static string? GetLayer(string projectName, string relativePath, SolutionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var path = NormalizePath(relativePath);
        if (!IsWithin(path, NormalizePath(layout.Root))) return null;
        if (layout.Mode == LayeringMode.Prefix) return projectName.Split('.')[0];

        foreach (var (folder, layer) in layout.ApplicationFolders.OrderByDescending(entry => entry.Key.Length))
        {
            if (IsWithin(path, NormalizePath(folder))) return layer;
        }

        return layout.LibraryRoot is { } library ? GetFolderLayer(path, library) ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// The prefix of the project's name (up to the first dot) for projects under <paramref name="root"/>, a
    /// solution-relative folder; <c>null</c> (not checked) for anything else.
    /// </summary>
    /// <param name="projectName">The project name.</param>
    /// <param name="relativePath">The project file's path relative to the solution directory.</param>
    /// <param name="root">The solution-relative folder holding the layered projects, e.g. <c>src</c>.</param>
    /// <returns>The layer, or <see langword="null"/> when the project is not checked.</returns>
    public static string? GetPrefixLayer(string projectName, string relativePath, string root) =>
        PathUnderRoot(relativePath, root) is not null ? projectName.Split('.')[0] : null;

    /// <summary>
    /// The first folder under <paramref name="root"/>, a solution-relative folder, or an empty string for a project
    /// directly under it; <c>null</c> (not checked) for projects outside it. Never falls back to the project name.
    /// </summary>
    /// <param name="relativePath">The project file's path relative to the solution directory.</param>
    /// <param name="root">The solution-relative folder holding the layer folders.</param>
    /// <returns>The layer folder, an empty string, or <see langword="null"/> when the project is not checked.</returns>
    public static string? GetFolderLayer(string relativePath, string root)
    {
        var path = PathUnderRoot(relativePath, root);
        if (path is null)
        {
            return null;
        }

        var parts = path.Split('/');
        return parts.Length > 1 ? parts[0] : string.Empty;
    }

    /// <summary>
    /// A solution-relative path with forward slashes, without leading <c>./</c> or trailing slashes; <c>.</c> for the
    /// solution directory itself.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimEnd('/');
        while (normalized.StartsWith("./", StringComparison.Ordinal)) normalized = normalized[2..];
        return normalized is "" ? "." : normalized;
    }

    /// <summary>
    /// Whether a solution-relative path is <paramref name="folder"/> or inside it, ignoring case; every path inside
    /// the solution is within <c>.</c>.
    /// </summary>
    /// <param name="path">The normalized path (see <see cref="NormalizePath"/>).</param>
    /// <param name="folder">The normalized folder.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is in <paramref name="folder"/>.</returns>
    public static bool IsWithin(string path, string folder) => folder == "."
        ? path != ".." && !path.StartsWith("../", StringComparison.Ordinal)
        : string.Equals(path, folder, StringComparison.OrdinalIgnoreCase) ||
          path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);

    private static string? PathUnderRoot(string relativePath, string root)
    {
        var path = relativePath.Replace('\\', '/');
        var prefix = root.Replace('\\', '/').TrimEnd('/');
        while (prefix.StartsWith("./", StringComparison.Ordinal)) prefix = prefix[2..];
        while (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
        if (prefix is "" or ".") return path.StartsWith("../", StringComparison.Ordinal) ? null : path;
        prefix += "/";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? path[prefix.Length..] : null;
    }

    /// <summary>
    /// Reads the projects with a layer (see <paramref name="layerOf"/>), ordered by name. References are reduced to
    /// project names, and sources and root folders are read from disk.
    /// </summary>
    /// <param name="solutionProjects">The solution's projects.</param>
    /// <param name="layerOf">Gives a project's layer, or <see langword="null"/> to leave it out.</param>
    /// <returns>The projects to check.</returns>
    public static IReadOnlyList<LayeringProject> Collect(IEnumerable<SolutionProjectInfo> solutionProjects, Func<SolutionProjectInfo, string?> layerOf)
    {
        var projects = solutionProjects
            .Select(project => (Project: project, Layer: layerOf(project)))
            .Where(x => x.Layer is not null)
            .OrderBy(x => x.Project.Name, StringComparer.Ordinal)
            .ToArray();

        return projects
            .Select(x => new LayeringProject(
                x.Project.Name,
                x.Layer!,
                x.Project.RootNamespace,
                x.Project.ProjectReferences
                    .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
                    .ToArray(),
                ReadSources(x.Project.Directory),
                Directory.EnumerateDirectories(x.Project.Directory).Select(Path.GetFileName).ToArray()!))
            .ToArray();
    }

    /// <summary>
    /// Returns every layering violation, one line each, in a stable order:
    /// <list type="number">
    ///   <item>projects without a known layer;</item>
    ///   <item>project references to a higher layer (or from a layer to a test kit);</item>
    ///   <item>source files importing a namespace owned by a project of a higher layer, which also catches upward
    ///   dependencies that only reach a project transitively;</item>
    ///   <item>root folders named after the project's own layer or a higher one: a project's own code sits at its
    ///   root, and only code for a lower layer gets a layer folder.</item>
    /// </list>
    /// Test kits are only checked for their layer; their files and folders are free.
    /// </summary>
    /// <param name="projects">The projects, as returned by <see cref="Collect"/>.</param>
    /// <param name="layers">The layers, innermost first.</param>
    /// <returns>The violations; empty when the solution follows its layering.</returns>
    public static IReadOnlyList<string> Check(IReadOnlyCollection<LayeringProject> projects, IReadOnlyList<string> layers) =>
        Check(projects, layers, $"no known layer prefix ({string.Join(", ", layers.Append(Testing))})");

    /// <summary>
    /// As <see cref="Check(IReadOnlyCollection{LayeringProject}, IReadOnlyList{string})"/>, against the layers of a
    /// solution <paramref name="layout"/>; in <see cref="LayeringMode.Folder"/>, a project without a known layer is
    /// reported as missing from the layer folders and application folders, never checked by its name instead.
    /// </summary>
    /// <param name="projects">The projects, as returned by <see cref="Collect"/>.</param>
    /// <param name="layout">The solution's layout.</param>
    /// <returns>The violations; empty when the solution follows its layering.</returns>
    public static IReadOnlyList<string> Check(IReadOnlyCollection<LayeringProject> projects, SolutionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var known = string.Join(", ", layout.Layers.Append(Testing));
        return layout.Mode == LayeringMode.Folder
            ? Check(projects, layout.Layers,
                $"no known layer folder ({known}): move it into a layer folder of {layout.LibraryRoot} or map its folder in applicationFolders")
            : Check(projects, layout.Layers, $"no known layer prefix ({known})");
    }

    private static IReadOnlyList<string> Check(IReadOnlyCollection<LayeringProject> projects, IReadOnlyList<string> layers, string unknownLayer)
    {
        var violations = new List<string>();
        var known = new List<LayeringProject>();

        foreach (var project in projects)
        {
            if (project.Layer == Testing || layers.Contains(project.Layer))
            {
                known.Add(project);
            }
            else
            {
                violations.Add($"project {project.Name} has {unknownLayer}");
            }
        }

        int Rank(string layer) => layer == Testing ? layers.Count : layers.ToList().IndexOf(layer);

        var layerByName = known.ToDictionary(p => p.Name, p => p.Layer);
        foreach (var project in known)
        {
            foreach (var target in project.References)
            {
                if (layerByName.TryGetValue(target, out var targetLayer) && Rank(targetLayer) > Rank(project.Layer))
                {
                    violations.Add($"project {project.Name} ({project.Layer}) -> {target} ({targetLayer})");
                }
            }
        }

        // Longest first, so a namespace is owned by its most specific project (Core.Events over Core).
        var owners = known.OrderByDescending(p => p.RootNamespace.Length).ToArray();
        string? LayerOfNamespace(string ns) => owners
            .FirstOrDefault(p => ns == p.RootNamespace || ns.StartsWith(p.RootNamespace + ".", StringComparison.Ordinal))?.Layer;

        foreach (var project in known.Where(p => p.Layer != Testing))
        {
            foreach (var (file, content) in project.Sources.OrderBy(s => s.Key, StringComparer.Ordinal))
            {
                var namespaces = UsingRegex().Matches(content).Select(m => m.Groups[1].Value).Distinct().Order(StringComparer.Ordinal);
                foreach (var ns in namespaces)
                {
                    if (LayerOfNamespace(ns) is { } target && Rank(target) > Rank(project.Layer))
                    {
                        violations.Add($"file {project.Name}/{file} ({project.Layer}) uses {ns} ({target})");
                    }
                }
            }
        }

        foreach (var project in known.Where(p => p.Layer != Testing))
        {
            foreach (var folder in layers.Skip(Rank(project.Layer)).Where(project.RootFolders.Contains))
            {
                violations.Add($"folder {project.Name}/{folder}/ ({project.Layer}) is not below the project's layer");
            }
        }

        return violations;
    }

    private static Dictionary<string, string> ReadSources(string directory) =>
        Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Select(file => (Relative: Path.GetRelativePath(directory, file), File: file))
            .Where(x => x.Relative.Split(Path.DirectorySeparatorChar)[0] is not ("bin" or "obj"))
            .ToDictionary(x => x.Relative.Replace('\\', '/'), x => File.ReadAllText(x.File));

    /// <summary>
    /// <c>using X;</c>, <c>global using X;</c>, <c>using static X;</c> and <c>using A = X;</c>, capturing <c>X</c>.
    /// </summary>
    [GeneratedRegex(@"^\s*(?:global\s+)?using\s+(?:static\s+)?(?:\w+\s*=\s*)?([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex UsingRegex();
}
