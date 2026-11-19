using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Build.Infrastructure.Helper;
using Anarchy.Toolkit.Automation.Core.Service;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Solutions;
using Serilog;
using Spectre.Console;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Packs source projects and publishes their NuGet packages in dependency order.
/// </summary>
/// <remarks>
/// Besides packing and publishing, it maintains package versions: <see cref="GenerateLocalRelease"/>,
/// <see cref="GenerateReleaseFromChanges"/>, <see cref="GenerateReleaseNotes"/> and <see cref="ValidateLocalRelease"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// partial class Build : FalloutBuild, INuGetBuild;
/// </code>
/// Then run <c>fallout publish --nu-get-source https://api.nuget.org/v3/index.json --nu-get-api-key $KEY</c>.
/// </example>
public interface INuGetBuild : IStandardBuild
{
    /// <summary>
    /// The NuGet feed URL or local directory <see cref="Publish"/> pushes to (<c>--nu-get-source</c>); required by it.
    /// </summary>
    [Parameter("NuGet feed URL or local directory to push packages to")]
    string? NuGetSource => TryGetValue(() => NuGetSource);

    /// <summary>
    /// The API key for <see cref="NuGetSource"/> (<c>--nu-get-api-key</c>); optional when configured credentials are used.
    /// </summary>
    [Parameter("API key for the NuGet feed (optional when using configured credentials)")]
    string? NuGetApiKey => TryGetValue(() => NuGetApiKey);

    /// <summary>
    /// Whether package versions already on the feed are skipped rather than failing the push (<c>--skip-duplicate</c>).
    /// Defaults to <see langword="true"/>.
    /// </summary>
    [Parameter("Skip package versions already present on the feed")]
    bool SkipDuplicate => TryGetValue<bool?>(() => SkipDuplicate) ?? true;

    /// <summary>
    /// The project <see cref="GenerateLocalRelease"/> bumps (<c>--release-project</c>), e.g. <c>Core.Events</c>.
    /// </summary>
    [Parameter("Project name to release (for example Core.Events)")]
    string? ReleaseProject => TryGetValue(() => ReleaseProject);

    /// <summary>
    /// The semantic version increment (<c>--release-bump</c>): <c>major</c>, <c>minor</c> or <c>patch</c>. Required by
    /// <see cref="GenerateLocalRelease"/>; <see cref="GenerateReleaseFromChanges"/> defaults to <c>patch</c>.
    /// </summary>
    [Parameter("Semantic version increment: major, minor or patch")]
    string? ReleaseBump => TryGetValue(() => ReleaseBump);

    /// <summary>
    /// The Git commit or ref to compare against (<c>--release-base</c>), e.g. the last released tag. Required by
    /// <see cref="GenerateReleaseFromChanges"/>, <see cref="GenerateReleaseNotes"/> and <see cref="ValidateLocalRelease"/>.
    /// </summary>
    [Parameter("Git commit or ref to compare package versions against")]
    string? ReleaseBase => TryGetValue(() => ReleaseBase);

    /// <summary>
    /// The Git commit or ref of the head of the changes (<c>--release-head</c>); defaults to <c>HEAD</c>.
    /// </summary>
    [Parameter("Git commit or ref for head of changes (defaults to HEAD)")]
    string? ReleaseHead => TryGetValue(() => ReleaseHead);

    /// <summary>
    /// A fresh output directory per build invocation, so earlier packages cannot be published accidentally.
    /// </summary>
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages" / NuGetPackageTasks.InvocationId;

    /// <summary>
    /// Source project files to pack: every <c>*.csproj</c> under <see cref="IDefaultBuild.SourceDirectory"/>
    /// outside <c>bin</c> and <c>obj</c>, including projects not yet in the solution. Override to change the selection.
    /// </summary>
    IEnumerable<AbsolutePath> NuGetProjects => Directory.EnumerateFiles(SourceDirectory, "*.csproj", SearchOption.AllDirectories)
        .Where(path => !Path.GetRelativePath(SourceDirectory, path).Split(Path.DirectorySeparatorChar)
            .Any(part => part is "bin" or "obj"))
        .Order(StringComparer.Ordinal)
        .Select(AbsolutePath.Create);

    /// <summary>
    /// Lists the projects selected by <see cref="NuGetProjects"/> with their package ID and version.
    /// </summary>
    Target ListPackages => d => d
        .Description("Lists source projects selected for NuGet packing")
        .Executes(() => {
            var table = BuildConsole.Table("Project", "Package ID", "Version");
            foreach (var project in NuGetProjects)
            {
                var solutionProject = Solution.AllProjects.FirstOrDefault(p => p.Path == project);
                var version = solutionProject?.GetProperty("Version");
                table.AddRow($"[grey]{Markup.Escape(RootDirectory.GetRelativePathTo(project).ToString())}[/]",
                    Markup.Escape(solutionProject?.GetProperty("PackageId") ?? Path.GetFileNameWithoutExtension(project)),
                    version is null ? "[grey](MSBuild default)[/]" : $"[green]{Markup.Escape(version)}[/]");
            }

            BuildConsole.Write(table);
        });

    /// <summary>
    /// Bumps the version of <see cref="ReleaseProject"/> by <see cref="ReleaseBump"/>, and the versions of all its
    /// transitive package dependents, editing the project files.
    /// </summary>
    Target GenerateLocalRelease => d => d
        .Description("Bumps a project's version and all its transitive package dependents")
        .Requires(() => ReleaseProject)
        .Requires(() => ReleaseBump)
        .Executes(() => {
            if (!Enum.TryParse<ReleaseVersionTasks.Bump>(ReleaseBump, true, out var bump) ||
                !Enum.IsDefined(bump) || !string.Equals(ReleaseBump, bump.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Release bump must be major, minor or patch.");
            var projects = ReleaseVersionTasks.ReadProjects(NuGetProjects.Select(p => p.ToString()), File.ReadAllText);
            var edits = ReleaseVersionTasks.Plan(projects, ReleaseProject!, bump);
            foreach (var (path, content) in edits) File.WriteAllText(path, content);
            ShowReleaseSummary(projects, edits, [ReleaseProject!]);
        });

    /// <summary>
    /// Detects the projects changed between <see cref="ReleaseBase"/> and <see cref="ReleaseHead"/>, and bumps their
    /// versions and those of their transitive dependents by <see cref="ReleaseBump"/> (<c>patch</c> by default). The
    /// working tree must match the head; the result is validated against the baseline before any file is written.
    /// </summary>
    Target GenerateReleaseFromChanges => d => d
        .Description("Detects changed projects between two commits and bumps their versions and transitive dependents")
        .Requires(() => ReleaseBase)
        .DependsOn(Restore)
        .Executes(() => {
            var bumpStr = ReleaseBump ?? "patch";
            if (!Enum.TryParse<ReleaseVersionTasks.Bump>(bumpStr, true, out var bump) ||
                !Enum.IsDefined(bump) || !string.Equals(bumpStr, bump.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Release bump must be major, minor or patch.");

            var headRef = string.IsNullOrWhiteSpace(ReleaseHead) ? "HEAD" : ReleaseHead;
            ReleaseVersionTasks.VerifyWorkingTree(RootDirectory, headRef);
            var projects = ReleaseVersionTasks.ReadProjects(NuGetProjects.Select(p => p.ToString()), File.ReadAllText);
            var baseCommit = ReleaseVersionTasks.ResolveCommit(RootDirectory, ReleaseBase!);
            var headCommit = ReleaseVersionTasks.ResolveCommit(RootDirectory, headRef);
            var changedFiles = ReleaseVersionTasks.GetChangedFiles(RootDirectory, baseCommit, headCommit);
            if (changedFiles.Contains("Directory.Packages.props", StringComparer.OrdinalIgnoreCase))
            {
                // The solution restore does not include source projects that are not in the solution.
                var solutionPaths = Solution.AllProjects.Select(p => p.Path.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var project in projects.Where(p => !solutionPaths.Contains(p.Path)))
                    DotNetRestore(s => s.SetProjectFile(project.Path).SetVerbosity(ToolVerbosity));
            }
            var direct = ReleaseVersionTasks.DetectDirectlyAffectedProjects(RootDirectory, projects, changedFiles,
                file => ReleaseVersionTasks.TryGitShow(RootDirectory, baseCommit, file),
                file => ReleaseVersionTasks.TryGitShow(RootDirectory, headCommit, file));
            var affected = direct.Count == 0 ? [] : ReleaseVersionTasks.Dependents(projects, direct.Select(p => p.Name));

            if (affected.Count == 0)
            {
                BuildConsole.Skipped($"No packages affected by changes between {ReleaseBase} and {headRef}.");
                return;
            }

            var baseline = ReleaseVersionTasks.ReadBaseline(RootDirectory, projects.Select(p => p.Path), ReleaseBase!);
            var edits = ReleaseVersionTasks.PlanChanges(projects, affected.Select(p => p.Name), bump, baseline);
            var updated = ReleaseVersionTasks.ReadProjects(projects.Select(p => p.Path), path => edits.TryGetValue(path, out var content) ? content : File.ReadAllText(path));
            var errors = ReleaseVersionTasks.Validate(updated, baseline);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            foreach (var (path, content) in edits) File.WriteAllText(path, content);
            ShowReleaseSummary(projects, edits, direct.Select(p => p.Name));
        });

    /// <summary>
    /// Adds missing package release notes from the commits between <see cref="ReleaseBase"/> and the checked-out
    /// <c>HEAD</c> that affect each package.
    /// </summary>
    Target GenerateReleaseNotes => d => d
        .Description("Adds missing package release notes from commits affecting each package")
        .Requires(() => ReleaseBase)
        .DependsOn(Restore)
        .Executes(() => {
            var head = string.IsNullOrWhiteSpace(ReleaseHead) ? "HEAD" : ReleaseHead;
            if (ReleaseVersionTasks.ResolveCommit(RootDirectory, head) != ReleaseVersionTasks.ResolveCommit(RootDirectory, "HEAD"))
                throw new InvalidOperationException("Release head must be the checked-out HEAD.");
            var projects = ReleaseVersionTasks.ReadProjects(NuGetProjects.Select(p => p.ToString()), File.ReadAllText);
            var solutionPaths = Solution.AllProjects.Select(p => p.Path.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var project in projects.Where(p => !solutionPaths.Contains(p.Path)))
                DotNetRestore(s => s.SetProjectFile(project.Path).SetVerbosity(ToolVerbosity));
            var messages = ReleaseNotesTasks.Collect(RootDirectory, projects, ReleaseBase!, head);
            var edits = ReleaseNotesTasks.Plan(projects, messages, path => File.Exists(path) ? File.ReadAllText(path) : null);
            if (edits.Count == 0)
            {
                BuildConsole.Skipped("Release notes are already up to date.");
                return;
            }

            var table = BuildConsole.Table("Release notes updated");
            foreach (var (path, content) in edits)
            {
                File.WriteAllText(path, content);
                table.AddRow($"[green]{Markup.Escape(RootDirectory.GetRelativePathTo(path).ToString())}[/]");
            }

            BuildConsole.Write(table);
        });

    /// <summary>
    /// Checks that the versions of packages and their dependents were bumped consistently against
    /// <see cref="ReleaseBase"/>, failing the build on any violation.
    /// </summary>
    Target ValidateLocalRelease => d => d
        .Description("Checks version bumps of package dependents against a Git baseline")
        .Requires(() => ReleaseBase)
        .Executes(() => {
            var projects = ReleaseVersionTasks.ReadProjects(NuGetProjects.Select(p => p.ToString()), File.ReadAllText);
            var baseline = ReleaseVersionTasks.ReadBaseline(RootDirectory, projects.Select(p => p.Path), ReleaseBase!);
            var errors = ReleaseVersionTasks.Validate(projects, baseline);
            foreach (var error in errors) Log.Error("{Violation}", error);
            if (errors.Count > 0) Assert.Fail($"{errors.Count} release version violations");
            Log.Information("Release versions validated against {Base}", ReleaseBase);
        });

    /// <summary>
    /// Runs the unit tests (<see cref="IStandardBuild.TestUnit"/>, the only kind that needs no local environment), then
    /// packs every project of <see cref="NuGetProjects"/> into <see cref="PackagesDirectory"/>, each in its own
    /// subdirectory, reusing the build of solution projects when <see cref="IStandardBuild.Compile"/> ran. Fails when no
    /// project is selected, no package is produced, or the packages cannot be ordered for publishing.
    /// </summary>
    Target Pack => d => d
        .Description("Packs source projects into a fresh artifacts/packages directory, after the unit tests")
        .DependsOn(Compile, TestUnit)
        .Executes(() => {
            var projects = NuGetProjects.ToArray();
            if (projects.Length == 0) throw new InvalidOperationException("No source projects selected for NuGet packing.");
            var solutionPaths = Solution.AllProjects.Select(p => p.Path.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            PackagesDirectory.CreateDirectory();
            for (var i = 0; i < projects.Length; i++)
            {
                var project = projects[i];
                var output = PackagesDirectory / i.ToString("D4");
                output.CreateDirectory();
                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetVerbosity(ToolVerbosity)
                    .SetNoBuild(IsBuilt && solutionPaths.Contains(project.ToString()))
                    .SetNoRestore(IsRestored && solutionPaths.Contains(project.ToString()))
                    .SetOutputDirectory(output));
            }

            var packages = NuGetPackageTasks.OrderForPublish(NuGetPackageTasks.FindPackages(PackagesDirectory));
            if (packages.Count == 0) throw new InvalidOperationException("No NuGet packages were produced.");
            Log.Information("Packed {Count} packages into {Directory}", packages.Count, PackagesDirectory);
        });

    /// <summary>
    /// Pushes the packages of <see cref="Pack"/> to <see cref="NuGetSource"/>, each after the packages of the batch it
    /// depends on (see <see cref="Tasks.NuGetPackageTasks.OrderForPublish"/>), stopping at the first failure. A summary
    /// table shows which packages were pushed, failed or not attempted.
    /// </summary>
    Target Publish => d => d
        .Description("Publishes NuGet packages after their packaged dependencies")
        .DependsOn(Pack)
        .Requires(() => NuGetSource)
        .Executes(() => {
            var packages = NuGetPackageTasks.OrderForPublish(NuGetPackageTasks.FindPackages(PackagesDirectory));
            var results = BuildConsole.Table("#", "Package", "Status");
            var index = 0;
            try
            {
                foreach (var package in packages)
                {
                    index++;
                    var name = Path.GetFileName(package);
                    BuildConsole.Section($"Pushing {index}/{packages.Count}: {name}");
                    try
                    {
                        DotNetNuGetPush(s => s
                            .SetTargetPath(package)
                            .SetSource(NuGetSource)
                            .SetApiKey(NuGetApiKey)
                            .SetSkipDuplicate(SkipDuplicate));
                    }
                    catch
                    {
                        results.AddRow(index.ToString(), Markup.Escape(name), "[red]failed[/]");
                        throw;
                    }

                    results.AddRow(index.ToString(), Markup.Escape(name), "[green]pushed[/]");
                }
            }
            finally
            {
                foreach (var package in packages.Skip(results.Rows.Count))
                    results.AddRow((++index).ToString(), Markup.Escape(Path.GetFileName(package)), "[grey]not attempted[/]");
                BuildConsole.Section($"Publish summary ({NuGetSource})");
                BuildConsole.Write(results);
            }
        });

    /// <summary>
    /// Prints the version changes in <paramref name="edits"/> as a table, marking the projects in
    /// <paramref name="changed"/> as changed and every other one as a dependent.
    /// </summary>
    private void ShowReleaseSummary(IReadOnlyCollection<ReleaseVersionTasks.Project> projects,
                                    IReadOnlyDictionary<string, string> edits, IEnumerable<string> changed)
    {
        var direct = changed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var updated = ReleaseVersionTasks.ReadProjects(edits.Keys, path => edits[path]).ToDictionary(p => p.Path);
        var table = BuildConsole.Table("Project", "Version", "Reason");
        foreach (var project in projects.Where(p => updated.ContainsKey(p.Path)).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            table.AddRow(Markup.Escape(project.Name),
                $"[grey]{Markup.Escape(project.Version)}[/] → [green]{Markup.Escape(updated[project.Path].Version)}[/]",
                direct.Contains(project.Name) ? "[yellow]changed[/]" : "[grey]dependent[/]");
        }

        BuildConsole.Section($"Bumped {updated.Count} packages");
        BuildConsole.Out.Write(table);
    }
}
