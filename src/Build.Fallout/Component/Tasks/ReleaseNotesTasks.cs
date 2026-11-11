using System.Text.RegularExpressions;
using static Anarchy.Toolkit.Build.Fallout.Component.Tasks.ReleaseVersionTasks;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    /// <summary>Builds package release notes from the commits affecting each package.</summary>
    public static class ReleaseNotesTasks
    {
        /// <summary>Collects commit subjects for the packages affected by a Git range, including dependents.</summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> Collect(
            string root, IReadOnlyCollection<Project> projects, string baseRef, string headRef)
        {
            var baseline = ResolveCommit(root, baseRef);
            var head = ResolveCommit(root, headRef);
            Git(root, "merge-base", "--is-ancestor", baseline, head);
            var affected = DetectAffectedProjects(root, projects, baseline, head)
                .ToDictionary(p => p.Path, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
            if (affected.Count == 0) return new Dictionary<string, IReadOnlyList<string>>();

            // NUL delimiters keep arbitrary commit subjects and paths separate. A merge is compared
            // to its first parent; commits on merged branches are also included in the range.
            var log = Git(root, "log", "--reverse", "--topo-order", "-z", "--format=%H%x00%P%x00%s", $"{baseline}..{head}");
            var fields = log.Length == 0 ? [] : log[..^1].Split('\0');
            if (fields.Length % 3 != 0) throw new InvalidOperationException("Could not parse Git commit history for release notes.");
            for (var i = 0; i < fields.Length; i += 3)
            {
                var commit = fields[i];
                var parent = fields[i + 1].Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                var subject = fields[i + 2].Trim();
                if (parent == null || subject.Length == 0 || subject == "chore(release): bump affected packages") continue;
                var commitProjects = DetectAffectedProjects(root, projects, parent, commit);
                foreach (var project in commitProjects)
                    if (affected.TryGetValue(project.Path, out var messages) && !messages.Contains(subject, StringComparer.Ordinal))
                        messages.Add(subject);
            }
            return affected.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Plans README edits without replacing existing entries or writing any files.</summary>
        public static IReadOnlyDictionary<string, string> Plan(
            IReadOnlyCollection<Project> projects, IReadOnlyDictionary<string, IReadOnlyList<string>> messages,
            Func<string, string?> read)
        {
            var edits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in projects.Where(p => messages.ContainsKey(p.Path)))
            {
                var path = Path.Combine(Path.GetDirectoryName(project.Path)!, "README.md");
                var content = read(path) ?? $"# {project.Name}\n";
                var updated = AddEntry(content, project.Version, messages[project.Path]);
                if (updated != content) edits.Add(path, updated);
            }
            return edits;
        }

        /// <summary>Adds an entry at the beginning of the Releases section, unless that version is already present.</summary>
        public static string AddEntry(string readme, string version, IReadOnlyList<string> subjects)
        {
            var newline = readme.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            var sectionStart = -1;
            var sectionEnd = readme.Length;
            var inFence = false;
            char fenceChar = '\0';
            var fenceLength = 0;
            var versionFound = false;
            foreach (Match line in Regex.Matches(readme, @"(?m)^[^\r\n]*(?:\r?\n|$)"))
            {
                if (line.Length == 0) continue;
                var text = line.Value.TrimEnd('\r', '\n');
                var fence = Regex.Match(text, @"^ {0,3}(`{3,}|~{3,})");
                if (fence.Success)
                {
                    var marker = fence.Groups[1].Value;
                    if (!inFence) { inFence = true; fenceChar = marker[0]; fenceLength = marker.Length; }
                    else if (marker[0] == fenceChar && marker.Length >= fenceLength) inFence = false;
                    continue;
                }
                if (inFence) continue;
                if (Regex.IsMatch(text, @"^ {0,3}##[ \t]+Releases[ \t]*#*[ \t]*$", RegexOptions.IgnoreCase))
                {
                    if (sectionStart >= 0) throw new InvalidOperationException("README contains more than one Releases section.");
                    sectionStart = line.Index + line.Length;
                    continue;
                }
                if (sectionStart < 0) continue;
                if (Regex.IsMatch(text, @"^ {0,3}#{1,2}[ \t]+")) { sectionEnd = line.Index; break; }
                if (Regex.IsMatch(text, @"^ {0,3}###[ \t]+\[?v?" + Regex.Escape(version) + @"(?=$|[\]\s#])", RegexOptions.IgnoreCase))
                    versionFound = true;
            }
            if (versionFound) return readme;
            var entries = subjects.Select(s => Regex.Replace(s.Trim(), @"[\r\n\x00-\x1f]+", " "))
                .Where(s => s.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
            if (entries.Length == 0) throw new InvalidOperationException($"No commit messages found for release {version}.");
            var entry = $"### {version}{newline}{newline}" + string.Join(newline, entries.Select(s => "- " + s)) + newline;
            if (sectionStart < 0)
            {
                var separator = readme.Length == 0 ? "" : readme.EndsWith(newline, StringComparison.Ordinal) ? newline : newline + newline;
                return readme + separator + "## Releases" + newline + newline + entry;
            }
            var prefix = sectionStart > 0 && readme[sectionStart - 1] != '\n' ? newline : "";
            var suffix = sectionStart == sectionEnd || !readme.AsSpan(sectionStart).StartsWith(newline) ? newline : "";
            return readme[..sectionStart] + prefix + newline + entry + suffix + readme[sectionStart..];
        }
    }
}
