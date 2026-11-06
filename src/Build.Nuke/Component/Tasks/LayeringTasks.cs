using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using Nuke.Common.ProjectModel;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    /// <summary>
    /// Checks that a solution follows its layered architecture (see <see cref="ILayeringBuild"/>): projects, source files
    /// and root folders only depend on their own or a lower layer.
    /// </summary>
    public static partial class LayeringTasks
    {
        /// <summary>The group of the test kits: they may reference any layer, but no layer may reference them.</summary>
        public const string Testing = "Testing";

        /// <summary>
        /// The toolkit's layered chain, innermost first (see the architecture guide). <c>Build</c> (the NUKE build
        /// components) sits on top so that no shipped project can depend on it.
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultLayers = ["Core", "Infrastructure", "Configuration", "Hosting", "Web", "Build"];

        /// <summary>
        /// See <see cref="ILayeringBuild.GetLayer"/>: the prefix of the project's name for projects under <c>src/</c>,
        /// <c>null</c> (not checked) for anything else.
        /// </summary>
        public static string? GetDefaultLayer(string projectName, string relativePath) => GetDefaultLayer(projectName, relativePath, "src");

        /// <summary>Gets the name prefix for projects under the specified solution-relative root.</summary>
        public static string? GetDefaultLayer(string projectName, string relativePath, string root) =>
            PathUnderRoot(relativePath, root) is not null ? projectName.Split('.')[0] : null;

        /// <summary>
        /// The first folder under <c>src/</c>, or an empty string for a project directly under <c>src/</c>;
        /// projects outside <c>src/</c> are not checked. Never falls back to the project name.
        /// </summary>
        public static string? GetFolderLayer(string relativePath) => GetFolderLayer(relativePath, "src");

        /// <summary>Gets the first folder under the specified solution-relative root.</summary>
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

        public static IReadOnlyList<LayeringProject> Collect(Solution solution, Func<Project, string?> layerOf)
        {
            var projects = solution.AllProjects
                .Select(project => (Project: project, Layer: layerOf(project)))
                .Where(x => x.Layer is not null)
                .OrderBy(x => x.Project.Name, StringComparer.Ordinal)
                .ToArray();

            return projects
                .Select(x => new LayeringProject(
                    x.Project.Name,
                    x.Layer!,
                    x.Project.GetProperty("RootNamespace") ?? x.Project.Name,
                    x.Project.GetItems<string>("ProjectReference")
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
        public static IReadOnlyList<string> Check(IReadOnlyCollection<LayeringProject> projects, IReadOnlyList<string> layers) =>
            Check(projects, layers, false);

        /// <summary>Checks layering and reports unknown layers as missing folder names when <paramref name="byFolder"/> is set.</summary>
        public static IReadOnlyList<string> Check(IReadOnlyCollection<LayeringProject> projects, IReadOnlyList<string> layers, bool byFolder)
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
                    violations.Add($"project {project.Name} has no known layer {(byFolder ? "folder" : "prefix")} ({string.Join(", ", layers.Append(Testing))})");
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

        /// <summary><c>using X;</c>, <c>global using X;</c>, <c>using static X;</c> and <c>using A = X;</c>, capturing <c>X</c>.</summary>
        [GeneratedRegex(@"^\s*(?:global\s+)?using\s+(?:static\s+)?(?:\w+\s*=\s*)?([\w.]+)\s*;", RegexOptions.Multiline)]
        private static partial Regex UsingRegex();
    }
}
