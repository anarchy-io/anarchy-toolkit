using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    /// <summary>Plans local semantic version bumps and checks their propagation through project references.</summary>
    public static class ReleaseVersionTasks
    {
        public enum Bump { Patch = 1, Minor = 2, Major = 3 }

        public sealed record Project(string Path, string Name, string Version, IReadOnlyList<string> References, string Content);

        /// <summary>Reads explicit versions and local project references without changing project formatting.</summary>
        public static IReadOnlyList<Project> ReadProjects(IEnumerable<string> paths, Func<string, string> read)
        {
            var files = paths.Select(Path.GetFullPath).Order(StringComparer.Ordinal).ToArray();
            if (files.Length == 0) throw new InvalidOperationException("No projects selected for versioning.");
            var selected = files.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (selected.Count != files.Length) throw new InvalidOperationException("Duplicate project paths selected for versioning.");
            return files.Select(path => {
                var content = read(path);
                var xml = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
                var versions = xml.Descendants().Where(e => e.Name.LocalName == "Version").ToArray();
                if (versions.Length != 1 || versions[0].AncestorsAndSelf().Any(e => e.Attribute("Condition") != null))
                    throw new InvalidOperationException($"{path} must have exactly one unconditional Version element.");
                var version = versions[0].Value.Trim();
                Parse(version);
                var references = xml.Descendants().Where(e => e.Name.LocalName == "ProjectReference")
                    .Select(e => (string?)e.Attribute("Include"))
                    .Where(include => include != null)
                    .Select(include => {
                        if (include!.IndexOfAny(['$', '@', '*', '?', ';']) >= 0)
                            throw new InvalidOperationException($"Unsupported project reference '{include}' in {path}.");
                        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, include.Replace('\\', Path.DirectorySeparatorChar)));
                    })
                    .Where(selected.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                return new Project(path, Path.GetFileNameWithoutExtension(path), version, references, content);
            }).ToArray();
        }

        /// <summary>Finds the selected project and all its transitive dependents, once each.</summary>
        public static IReadOnlyList<Project> Dependents(IReadOnlyCollection<Project> projects, string projectName)
        {
            var matches = projects.Where(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one project named '{projectName}', found {matches.Length}.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { matches[0].Path };
            var queue = new Queue<string>();
            queue.Enqueue(matches[0].Path);
            while (queue.TryDequeue(out var dependency))
                foreach (var project in projects.Where(p => p.References.Contains(dependency, StringComparer.OrdinalIgnoreCase)))
                    if (seen.Add(project.Path)) queue.Enqueue(project.Path);
            return projects.Where(p => seen.Contains(p.Path)).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
        }

        public static string Increment(string version, Bump bump)
        {
            var (major, minor, patch) = Parse(version);
            return bump switch {
                Bump.Major => $"{checked(major + 1)}.0.0",
                Bump.Minor => $"{major}.{checked(minor + 1)}.0",
                Bump.Patch => $"{major}.{minor}.{checked(patch + 1)}",
                _ => throw new ArgumentOutOfRangeException(nameof(bump))
            };
        }

        /// <summary>Prepares every edit before writing any file, preserving the rest of the project text.</summary>
        public static IReadOnlyDictionary<string, string> Plan(IReadOnlyCollection<Project> projects, string name, Bump bump)
        {
            var result = new Dictionary<string, string>();
            foreach (var project in Dependents(projects, name))
            {
                var pattern = @"(<Version>\s*)" + Regex.Escape(project.Version) + @"(\s*</Version>)";
                var matches = Regex.Matches(project.Content, pattern);
                if (matches.Count != 1) throw new InvalidOperationException($"Cannot safely update Version in {project.Path}.");
                var updated = Regex.Replace(project.Content, pattern, m => m.Groups[1].Value + Increment(project.Version, bump) + m.Groups[2].Value);
                result.Add(project.Path, updated);
            }
            return result;
        }

        /// <summary>Checks each existing bumped project and its transitive existing dependents against the baseline.</summary>
        public static IReadOnlyList<string> Validate(IReadOnlyCollection<Project> projects, IReadOnlyDictionary<string, string> baseline)
        {
            var changes = new Dictionary<string, Bump?>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();
            foreach (var project in projects)
            {
                if (!baseline.TryGetValue(project.Path, out var old)) continue; // New packages have no previous version.
                var before = Parse(old);
                var after = Parse(project.Version);
                if (after.CompareTo(before) < 0) errors.Add($"{project.Name}: version decreased from {old} to {project.Version}.");
                changes[project.Path] = after.CompareTo(before) <= 0 ? null
                    : after.Major != before.Major ? Bump.Major
                    : after.Minor != before.Minor ? Bump.Minor : Bump.Patch;
            }
            foreach (var project in projects.Where(p => changes.TryGetValue(p.Path, out var b) && b != null))
            {
                var required = changes[project.Path]!.Value;
                foreach (var dependent in Dependents(projects, project.Name).Where(p => p.Path != project.Path))
                    if (changes.TryGetValue(dependent.Path, out var actual) && (actual == null || actual < required))
                        errors.Add($"{dependent.Name}: needs at least a {required.ToString().ToLowerInvariant()} bump because it depends on {project.Name}.");
            }
            return errors;
        }

        /// <summary>Reads versions from a Git commit. Missing files are treated as new projects.</summary>
        public static IReadOnlyDictionary<string, string> ReadBaseline(string root, IEnumerable<string> paths, string revision)
        {
            var commit = Git(root, "rev-parse", "--verify", "--end-of-options", revision + "^{commit}").Trim();
            var tracked = Git(root, "ls-tree", "-r", "--name-only", "-z", commit).Split('\0', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.Ordinal);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths.Select(Path.GetFullPath))
            {
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                if (relative.StartsWith("../", StringComparison.Ordinal)) throw new InvalidOperationException($"Project is outside repository: {path}");
                if (!tracked.Contains(relative)) continue;
                var content = Git(root, "show", $"{commit}:{relative}");
                result.Add(path, ReadProjects([path], _ => content)[0].Version);
            }
            return result;
        }

        private static (int Major, int Minor, int Patch) Parse(string version)
        {
            if (!Regex.IsMatch(version, @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$") ||
                !int.TryParse(version.Split('.')[0], out var major) || !int.TryParse(version.Split('.')[1], out var minor) ||
                !int.TryParse(version.Split('.')[2], out var patch))
                throw new InvalidOperationException($"Version '{version}' must be a stable semantic version (major.minor.patch).");
            return (major, minor, patch);
        }

        private static string Git(string root, params string[] args)
        {
            var start = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in args) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start git.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();
            if (process.ExitCode != 0) throw new InvalidOperationException($"Git failed: {error.Trim()}");
            return output;
        }
    }
}
