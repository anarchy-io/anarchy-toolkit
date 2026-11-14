using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// Plans local semantic version bumps and checks their propagation through project references.
/// </summary>
public static class ReleaseVersionTasks
{
    /// <summary>
    /// A semantic version increment, ordered from smallest to largest.
    /// </summary>
    public enum Bump { Patch = 1, Minor = 2, Major = 3 }

    /// <summary>
    /// A versioned project.
    /// </summary>
    /// <param name="Path">The full path of the project file.</param>
    /// <param name="Name">The project file name without extension.</param>
    /// <param name="Version">Its <c>major.minor.patch</c> version.</param>
    /// <param name="References">Full paths of the referenced projects, among those read together.</param>
    /// <param name="Content">The project file's text.</param>
    public sealed record Project(string Path, string Name, string Version, IReadOnlyList<string> References, string Content);

    /// <summary>
    /// Reads explicit versions and local project references without changing project formatting. Only references to
    /// other projects in <paramref name="paths"/> are kept.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no or duplicate paths are given, a project lacks exactly one unconditional stable <c>Version</c>, or
    /// a reference uses MSBuild properties, items or wildcards.
    /// </exception>
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

    /// <summary>
    /// Finds the selected projects and all their transitive dependents, once each, ordered by name.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a name does not match exactly one project.</exception>
    public static IReadOnlyList<Project> Dependents(IReadOnlyCollection<Project> projects, IEnumerable<string> projectNames)
    {
        var nameList = projectNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (nameList.Length == 0) return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();

        foreach (var name in nameList)
        {
            var matches = projects.Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Expected one project named '{name}', found {matches.Length}.");

            if (seen.Add(matches[0].Path))
                queue.Enqueue(matches[0].Path);
        }

        while (queue.TryDequeue(out var dependency))
        {
            foreach (var project in projects.Where(p => p.References.Contains(dependency, StringComparer.OrdinalIgnoreCase)))
            {
                if (seen.Add(project.Path))
                    queue.Enqueue(project.Path);
            }
        }

        return projects.Where(p => seen.Contains(p.Path)).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Finds the selected project and all its transitive dependents, once each.
    /// </summary>
    public static IReadOnlyList<Project> Dependents(IReadOnlyCollection<Project> projects, string projectName) =>
        Dependents(projects, [projectName]);

    /// <summary>
    /// The version after a bump, resetting the lower components (e.g. a minor bump of <c>1.2.3</c> is <c>1.3.0</c>).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="version"/> is not a stable semantic version.</exception>
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

    /// <summary>
    /// Plans a <paramref name="bump"/> of the named projects and all their transitive dependents, without writing any
    /// file: the new content of each project, keyed by path, with only the <c>Version</c> changed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a name is unknown or a <c>Version</c> cannot be updated safely.</exception>
    public static IReadOnlyDictionary<string, string> Plan(IReadOnlyCollection<Project> projects, IEnumerable<string> names, Bump bump)
    {
        var result = new Dictionary<string, string>();
        foreach (var project in Dependents(projects, names))
        {
            var pattern = @"(<Version>\s*)" + Regex.Escape(project.Version) + @"(\s*</Version>)";
            var matches = Regex.Matches(project.Content, pattern);
            if (matches.Count != 1) throw new InvalidOperationException($"Cannot safely update Version in {project.Path}.");
            var updated = Regex.Replace(project.Content, pattern, m => m.Groups[1].Value + Increment(project.Version, bump) + m.Groups[2].Value);
            result.Add(project.Path, updated);
        }
        return result;
    }

    /// <summary>
    /// Plans a <paramref name="bump"/> of the named project and all its transitive dependents; see the overload taking several names.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Plan(IReadOnlyCollection<Project> projects, string name, Bump bump) =>
        Plan(projects, [name], bump);

    /// <summary>
    /// Plans automatic bumps like <c>Plan</c>, but skips projects whose version is already higher than in
    /// <paramref name="baseline"/> (keyed by project path); new projects are bumped.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a version is lower than its baseline.</exception>
    public static IReadOnlyDictionary<string, string> PlanChanges(IReadOnlyCollection<Project> projects,
        IEnumerable<string> names, Bump bump, IReadOnlyDictionary<string, string> baseline)
    {
        var affected = Dependents(projects, names);
        var edits = Plan(projects, affected.Select(p => p.Name), bump);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in affected)
        {
            if (baseline.TryGetValue(project.Path, out var old))
            {
                var comparison = Parse(project.Version).CompareTo(Parse(old));
                if (comparison < 0) throw new InvalidOperationException($"{project.Name}: version decreased from {old} to {project.Version}.");
                if (comparison > 0) continue;
            }
            result.Add(project.Path, edits[project.Path]);
        }
        return result;
    }

    /// <summary>
    /// Checks the versions against <paramref name="baseline"/> (keyed by project path): none may decrease, and each
    /// dependent of a bumped project must be bumped at least as much. Projects missing from the baseline are new and
    /// not checked. Returns one error per problem.
    /// </summary>
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

    /// <summary>
    /// Reads the versions of the projects at a Git revision, keyed by full project path. Projects not tracked at that
    /// revision are new and left out.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a project is outside the repository, or Git fails.</exception>
    public static IReadOnlyDictionary<string, string> ReadBaseline(string root, IEnumerable<string> paths, string revision)
    {
        var commit = ResolveCommit(root, revision);
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

    /// <summary>
    /// Resolves a Git revision or ref to an exact 40-character commit SHA.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the revision is blank, all zeros (as a new branch's push base), or not a commit.</exception>
    public static string ResolveCommit(string root, string revision)
    {
        if (string.IsNullOrWhiteSpace(revision) || revision.All(c => c == '0'))
            throw new InvalidOperationException("A valid Git commit is required for release detection.");
        return Git(root, "rev-parse", "--verify", "--end-of-options", revision + "^{commit}").Trim();
    }

    /// <summary>
    /// Requires <paramref name="head"/> to be the checked-out <c>HEAD</c>, with no uncommitted changes to the sources
    /// or shared build settings, and no untracked files under <c>src</c> or <c>scripts</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the working tree does not match the head.</exception>
    public static void VerifyWorkingTree(string root, string head)
    {
        var commit = ResolveCommit(root, head);
        if (commit != ResolveCommit(root, "HEAD"))
            throw new InvalidOperationException("Release head must be the checked-out HEAD; check out the requested commit first.");
        var changes = Git(root, "diff", "--name-only", "-z", commit, "--", "src", "Directory.Packages.props",
            "Directory.Build.props", "Directory.Build.targets", "global.json", "nuget.config", "scripts", "Makefile", ".gitignore", ".editorconfig");
        var untracked = Git(root, "ls-files", "--others", "--exclude-standard", "-z", "--", "src", "scripts");
        if (changes.Length != 0 || untracked.Length != 0) throw new InvalidOperationException("Release source or build settings have uncommitted changes. Commit or stash them before generating a release.");
    }

    /// <summary>
    /// Retrieves the repository-relative paths changed between two Git commits, with forward slashes as Git reports
    /// them; a rename counts as a deletion and an addition.
    /// </summary>
    public static IReadOnlyList<string> GetChangedFiles(string root, string baseRef, string headRef = "HEAD")
    {
        var baseCommit = ResolveCommit(root, baseRef);
        var headCommit = ResolveCommit(root, headRef);
        var output = Git(root, "diff", "--name-only", "--no-renames", "-z", baseCommit, headCommit, "--");
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Attempts to read a file from a Git commit, returning null if the file does not exist in that commit.
    /// </summary>
    public static string? TryGitShow(string root, string commitOrRef, string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var commit = ResolveCommit(root, commitOrRef);
        var tracked = Git(root, "ls-tree", "-r", "--name-only", "-z", commit).Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
        return tracked.Contains(normalized) ? Git(root, "show", $"{commit}:{normalized}") : null;
    }

    /// <summary>
    /// Parses the package IDs and versions of a <c>Directory.Packages.props</c>; empty content yields none.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a <c>PackageVersion</c> has no ID or version, or is duplicated.</exception>
    public static IReadOnlyDictionary<string, string> ParsePackageVersions(string xmlContent)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(xmlContent)) return result;
        var doc = XDocument.Parse(xmlContent);
        var packageVersions = doc.Descendants().Where(e => e.Name.LocalName == "PackageVersion");
        foreach (var elem in packageVersions)
        {
            var id = (string?)elem.Attribute("Include") ?? (string?)elem.Attribute("Update");
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Unsupported PackageVersion without an Include or Update.");
            var version = (string?)elem.Attribute("Version") ?? elem.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;
            if (string.IsNullOrWhiteSpace(version) || !result.TryAdd(id.Trim(), version.Trim()))
                throw new InvalidOperationException($"Unsupported or duplicate central package version for {id}.");
        }
        return result;
    }

    /// <summary>
    /// Detects the package IDs added, removed, or with a changed version between two <c>Directory.Packages.props</c>
    /// snapshots; null means the file is absent.
    /// </summary>
    public static IReadOnlySet<string> DetectPackageVersionChanges(string? baseXml, string? headXml)
    {
        var basePackages = baseXml != null ? ParsePackageVersions(baseXml) : new Dictionary<string, string>();
        var headPackages = headXml != null ? ParsePackageVersions(headXml) : new Dictionary<string, string>();

        var changed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, headVer) in headPackages)
        {
            if (!basePackages.TryGetValue(id, out var baseVer) || !string.Equals(headVer, baseVer, StringComparison.OrdinalIgnoreCase))
            {
                changed.Add(id);
            }
        }
        foreach (var id in basePackages.Keys)
        {
            if (!headPackages.ContainsKey(id))
            {
                changed.Add(id);
            }
        }
        return changed;
    }

    /// <summary>
    /// Extracts the IDs of all packages, direct and transitive, in a restored <c>project.assets.json</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the content is empty or has no <c>libraries</c> object.</exception>
    public static IReadOnlySet<string> ReadConsumedPackagesFromAssets(string assetsJsonContent)
    {
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(assetsJsonContent)) throw new InvalidOperationException("Restored assets file is empty.");
        using var doc = JsonDocument.Parse(assetsJsonContent);
        if (!doc.RootElement.TryGetProperty("libraries", out var libraries) || libraries.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Restored assets file has no libraries object.");
        {
            foreach (var property in libraries.EnumerateObject())
            {
                if (property.Value.TryGetProperty("type", out var type) && string.Equals(type.GetString(), "package", StringComparison.OrdinalIgnoreCase))
                {
                    var slash = property.Name.IndexOf('/');
                    var packageId = slash >= 0 ? property.Name.Substring(0, slash) : property.Name;
                    packages.Add(packageId);
                }
            }
        }
        return packages;
    }

    /// <summary>
    /// Extracts the IDs of the <c>PackageReference</c> items (<c>Include</c> or <c>Update</c>) of a project file.
    /// </summary>
    public static IReadOnlySet<string> ReadDirectPackageReferences(string projectContent)
    {
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(projectContent)) return packages;
        var doc = XDocument.Parse(projectContent);
        foreach (var elem in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
        {
            var id = (string?)elem.Attribute("Include") ?? (string?)elem.Attribute("Update");
            if (!string.IsNullOrWhiteSpace(id)) packages.Add(id.Trim());
        }
        return packages;
    }

    /// <summary>
    /// Returns all package IDs consumed directly or transitively by a project. <paramref name="assetsJsonReader"/>
    /// defaults to reading <c>obj/project.assets.json</c> next to the project.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the project has not been restored.</exception>
    public static IReadOnlySet<string> GetConsumedPackages(
        Project project,
        Func<Project, string?>? assetsJsonReader = null)
    {
        var packages = new HashSet<string>(ReadDirectPackageReferences(project.Content), StringComparer.OrdinalIgnoreCase);
        var assetsContent = assetsJsonReader != null
            ? assetsJsonReader(project)
            : DefaultAssetsJsonReader(project);

        if (assetsContent == null) throw new InvalidOperationException($"Missing restored assets for {project.Path}. Restore all selected projects before detecting dependency changes.");
        foreach (var pkg in ReadConsumedPackagesFromAssets(assetsContent)) packages.Add(pkg);
        return packages;
    }

    private static string? DefaultAssetsJsonReader(Project project)
    {
        var assetsPath = Path.Combine(Path.GetDirectoryName(project.Path)!, "obj", "project.assets.json");
        return File.Exists(assetsPath) ? File.ReadAllText(assetsPath) : null;
    }

    /// <summary>
    /// Whether a changed repository-relative path cannot affect a package: anything under <c>tests</c>, <c>samples</c>,
    /// <c>build</c>, <c>docs</c>, <c>scripts</c> or tooling folders, and root files such as <c>README.md</c>,
    /// <c>Makefile</c> or the solution.
    /// </summary>
    public static bool IsIgnoredFile(string file)
    {
        var normalized = file.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("tests/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("samples/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("build/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(".github/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("docs/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(".fallout/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(".idea/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(".vscode/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var fileName = Path.GetFileName(normalized);
        if (!normalized.Contains('/') && (string.Equals(fileName, ".editorconfig", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, ".gitignore", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "README.md", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "LICENSE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "Makefile", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".sln.DotSettings.user", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Whether a changed path (in any folder) is shared build configuration that conservatively affects all packages:
    /// <c>Directory.Build.props</c>, <c>Directory.Build.targets</c>, <c>global.json</c> or <c>nuget.config</c>.
    /// </summary>
    public static bool IsAffectAllFile(string file)
    {
        var fileName = Path.GetFileName(file.Replace('\\', '/'));
        return string.Equals(fileName, "Directory.Build.props", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(fileName, "Directory.Build.targets", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(fileName, "global.json", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(fileName, "nuget.config", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Identifies the projects directly affected by changed files or package version updates, before transitive
    /// resolution, ordered by name. Shared build settings, unknown <c>.props</c>/<c>.targets</c> or <c>src</c> files,
    /// and <c>Directory.Packages.props</c> changes that cannot be attributed to specific packages affect every project.
    /// The file readers default to <c>HEAD~1</c> and <c>HEAD</c> (falling back to the working tree).
    /// </summary>
    public static IReadOnlyList<Project> DetectDirectlyAffectedProjects(
        string root,
        IReadOnlyCollection<Project> projects,
        IReadOnlyList<string> changedFiles,
        Func<string, string?>? baseFileReader = null,
        Func<string, string?>? headFileReader = null,
        Func<Project, string?>? assetsJsonReader = null)
    {
        var fullRoot = Path.GetFullPath(root);
        var directlyAffected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var affectAll = false;
        var packagesPropsChanged = false;

        // Map each project to its relative directory and relative project file path
        var projectMappings = projects.Select(p => {
            var projDir = Path.GetFullPath(Path.GetDirectoryName(p.Path)!);
            var relProjDir = Path.GetRelativePath(fullRoot, projDir).Replace('\\', '/').Trim('/');
            var relProjFile = Path.GetRelativePath(fullRoot, p.Path).Replace('\\', '/');
            return (Project: p, RelDir: relProjDir, RelFile: relProjFile);
        }).OrderByDescending(x => x.RelDir.Length).ToArray();

        foreach (var rawFile in changedFiles)
        {
            var file = rawFile.Replace('\\', '/').TrimStart('/');
            // Build.Cli embeds the repository's dotfiles; its Makefile and scripts live in its own folder.
            if (file is ".gitignore" or ".editorconfig")
            {
                foreach (var project in projects.Where(p => p.Content.Contains("Anarchy.Boilerplate/", StringComparison.Ordinal)))
                    directlyAffected.Add(project.Path);
            }
            if (IsIgnoredFile(file)) continue;

            if (IsAffectAllFile(file))
            {
                affectAll = true;
                continue;
            }

            if (string.Equals(file, "Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
            {
                packagesPropsChanged = true;
                continue;
            }

            Project? matchedProject = null;
            foreach (var mapping in projectMappings)
            {
                if (string.Equals(file, mapping.RelFile, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(mapping.RelDir) && file.StartsWith(mapping.RelDir + "/", StringComparison.OrdinalIgnoreCase)))
                {
                    matchedProject = mapping.Project;
                    break;
                }
            }

            if (matchedProject != null)
            {
                directlyAffected.Add(matchedProject.Path);
            }
            else if (file.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) ||
                     file.StartsWith("src/", StringComparison.OrdinalIgnoreCase))
            {
                // "Unknown shared build configuration changes should conservatively affect all packages."
                affectAll = true;
            }
        }

        if (packagesPropsChanged && !affectAll)
        {
            baseFileReader ??= rel => TryGitShow(fullRoot, "HEAD~1", rel);
            headFileReader ??= rel => TryGitShow(fullRoot, "HEAD", rel) ?? (File.Exists(Path.Combine(fullRoot, rel)) ? File.ReadAllText(Path.Combine(fullRoot, rel)) : null);

            var baseProps = baseFileReader("Directory.Packages.props");
            var headProps = headFileReader("Directory.Packages.props");
            if (new[] { baseProps, headProps }.Where(x => x != null).Any(x => XDocument.Parse(x!).Descendants()
                .Where(e => e.Name.LocalName == "PackageVersion")
                .Any(e => e.AncestorsAndSelf().Any(a => a.Attribute("Condition") != null) ||
                    e.Attribute("Remove") != null)))
                return projects.OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
            var changedPackages = DetectPackageVersionChanges(baseProps, headProps);

            // Changes outside the version items, conditional declarations, and removed pins cannot
            // be evaluated safely from the head restore alone. Release conservatively in those cases.
            static string OtherSettings(string? xml)
            {
                if (xml == null) return "";
                var document = XDocument.Parse(xml);
                foreach (var item in document.Descendants().Where(e => e.Name.LocalName == "PackageVersion").ToArray()) item.Remove();
                return document.ToString(SaveOptions.DisableFormatting);
            }
            static Dictionary<string, string> PackageMetadata(string? xml)
            {
                if (xml == null) return new(StringComparer.OrdinalIgnoreCase);
                var document = XDocument.Parse(xml);
                foreach (var item in document.Descendants().Where(e => e.Name.LocalName == "PackageVersion").ToArray())
                {
                    item.Attribute("Version")?.Remove();
                    item.Elements().Where(e => e.Name.LocalName == "Version").Remove();
                }
                return document.Descendants().Where(e => e.Name.LocalName == "PackageVersion")
                    .ToDictionary(e => ((string?)e.Attribute("Include") ?? (string)e.Attribute("Update")!).Trim(),
                        e => e.ToString(SaveOptions.DisableFormatting), StringComparer.OrdinalIgnoreCase);
            }
            var before = baseProps == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : ParsePackageVersions(baseProps);
            var after = headProps == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : ParsePackageVersions(headProps);
            var beforeMetadata = PackageMetadata(baseProps);
            var afterMetadata = PackageMetadata(headProps);
            if (OtherSettings(baseProps) != OtherSettings(headProps) || before.Keys.Except(after.Keys, StringComparer.OrdinalIgnoreCase).Any() ||
                beforeMetadata.Keys.Intersect(afterMetadata.Keys, StringComparer.OrdinalIgnoreCase).Any(id => beforeMetadata[id] != afterMetadata[id]))
                affectAll = true;

            if (changedPackages.Count > 0 && !affectAll)
            {
                foreach (var project in projects)
                {
                    var consumed = GetConsumedPackages(project, assetsJsonReader);
                    if (consumed.Overlaps(changedPackages))
                    {
                        directlyAffected.Add(project.Path);
                    }
                }
            }
        }

        if (affectAll)
        {
            return projects.OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
        }

        return projects.Where(p => directlyAffected.Contains(p.Path))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Detects directly and transitively affected projects from a list of changed files.
    /// </summary>
    public static IReadOnlyList<Project> DetectAffectedProjects(
        string root,
        IReadOnlyCollection<Project> projects,
        IReadOnlyList<string> changedFiles,
        Func<string, string?>? baseFileReader = null,
        Func<string, string?>? headFileReader = null,
        Func<Project, string?>? assetsJsonReader = null)
    {
        var directlyAffected = DetectDirectlyAffectedProjects(
            root,
            projects,
            changedFiles,
            baseFileReader,
            headFileReader,
            assetsJsonReader);

        if (directlyAffected.Count == 0) return [];

        return Dependents(projects, directlyAffected.Select(p => p.Name));
    }

    /// <summary>
    /// Detects directly and transitively affected projects between two Git commits.
    /// </summary>
    public static IReadOnlyList<Project> DetectAffectedProjects(
        string root,
        IReadOnlyCollection<Project> projects,
        string baseRef,
        string headRef = "HEAD",
        Func<Project, string?>? assetsJsonReader = null)
    {
        var baseCommit = ResolveCommit(root, baseRef);
        var headCommit = ResolveCommit(root, headRef);
        var changedFiles = GetChangedFiles(root, baseCommit, headCommit);

        return DetectAffectedProjects(
            root,
            projects,
            changedFiles,
            file => TryGitShow(root, baseCommit, file),
            file => TryGitShow(root, headCommit, file),
            assetsJsonReader);
    }

    private static (int Major, int Minor, int Patch) Parse(string version)
    {
        if (!Regex.IsMatch(version, @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$") ||
            !int.TryParse(version.Split('.')[0], out var major) || !int.TryParse(version.Split('.')[1], out var minor) ||
            !int.TryParse(version.Split('.')[2], out var patch))
            throw new InvalidOperationException($"Version '{version}' must be a stable semantic version (major.minor.patch).");
        return (major, minor, patch);
    }

    /// <summary>
    /// Runs <c>git</c> in <paramref name="root"/> and returns its output.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Git exits with a non-zero code.</exception>
    public static string Git(string root, params string[] args)
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
