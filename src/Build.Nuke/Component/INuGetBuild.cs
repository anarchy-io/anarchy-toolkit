using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    /// <summary>Packs source projects and publishes their NuGet packages in dependency order.</summary>
    public interface INuGetBuild : IStandardBuild
    {
        [Parameter("NuGet feed URL or local directory to push packages to")]
        string? NuGetSource => TryGetValue(() => NuGetSource);

        [Parameter("API key for the NuGet feed (optional when using configured credentials)")]
        string? NuGetApiKey => TryGetValue(() => NuGetApiKey);

        [Parameter("Skip package versions already present on the feed")]
        bool SkipDuplicate => TryGetValue<bool?>(() => SkipDuplicate) ?? true;

        [Parameter("Project name to release (for example Core.Events)")]
        string? ReleaseProject => TryGetValue(() => ReleaseProject);

        [Parameter("Semantic version increment: major, minor or patch")]
        string? ReleaseBump => TryGetValue(() => ReleaseBump);

        [Parameter("Git commit or ref to compare package versions against")]
        string? ReleaseBase => TryGetValue(() => ReleaseBase);

        [Parameter("Git commit or ref for head of changes (defaults to HEAD)")]
        string? ReleaseHead => TryGetValue(() => ReleaseHead);

        /// <summary>A fresh output directory per build invocation, so earlier packages cannot be published accidentally.</summary>
        AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages" / NuGetPackageTasks.InvocationId;

        /// <summary>Source project files to pack, including projects not yet in the solution. Override to change the selection.</summary>
        IEnumerable<AbsolutePath> NuGetProjects => Directory.EnumerateFiles(SourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(SourceDirectory, path).Split(Path.DirectorySeparatorChar)
                .Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal)
            .Select(AbsolutePath.Create);

        Target ListPackages => d => d
            .Description("Lists source projects selected for NuGet packing")
            .Executes(() => {
                foreach (var project in NuGetProjects)
                {
                    var solutionProject = Solution.AllProjects.FirstOrDefault(p => p.Path == project);
                    Log.Information("{Project}: {PackageId} {Version}", project,
                        solutionProject?.GetProperty("PackageId") ?? Path.GetFileNameWithoutExtension(project),
                        solutionProject?.GetProperty("Version") ?? "(MSBuild default)");
                }
            });

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
                foreach (var (path, content) in edits)
                {
                    File.WriteAllText(path, content);
                    Log.Information("Bumped {Project} ({Bump})", Path.GetFileNameWithoutExtension(path), bump);
                }
            });

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
                if (ReleaseVersionTasks.GetChangedFiles(RootDirectory, ReleaseBase!, headRef).Contains("Directory.Packages.props", StringComparer.OrdinalIgnoreCase))
                {
                    // The solution restore does not include source projects that are not in the solution.
                    var solutionPaths = Solution.AllProjects.Select(p => p.Path.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var project in projects.Where(p => !solutionPaths.Contains(p.Path)))
                        DotNetRestore(s => s.SetProjectFile(project.Path));
                }
                var affected = ReleaseVersionTasks.DetectAffectedProjects(RootDirectory, projects, ReleaseBase!, headRef);

                if (affected.Count == 0)
                {
                    Log.Information("No packages affected by changes between {Base} and {Head}", ReleaseBase, headRef);
                    return;
                }

                var baseline = ReleaseVersionTasks.ReadBaseline(RootDirectory, projects.Select(p => p.Path), ReleaseBase!);
                var edits = ReleaseVersionTasks.PlanChanges(projects, affected.Select(p => p.Name), bump, baseline);
                var updated = ReleaseVersionTasks.ReadProjects(projects.Select(p => p.Path), path => edits.TryGetValue(path, out var content) ? content : File.ReadAllText(path));
                var errors = ReleaseVersionTasks.Validate(updated, baseline);
                if (errors.Count > 0) throw new InvalidOperationException(string.Join(System.Environment.NewLine, errors));
                foreach (var (path, content) in edits)
                {
                    File.WriteAllText(path, content);
                    Log.Information("Bumped {Project} ({Bump})", Path.GetFileNameWithoutExtension(path), bump);
                }
            });

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
                    DotNetRestore(s => s.SetProjectFile(project.Path));
                var messages = ReleaseNotesTasks.Collect(RootDirectory, projects, ReleaseBase!, head);
                var edits = ReleaseNotesTasks.Plan(projects, messages, path => File.Exists(path) ? File.ReadAllText(path) : null);
                foreach (var (path, content) in edits)
                {
                    File.WriteAllText(path, content);
                    Log.Information("Updated release notes in {Readme}", path);
                }
            });

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

        Target Pack => d => d
            .Description("Packs source projects into a fresh artifacts/packages directory")
            .DependsOn(Compile)
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
                        .SetNoBuild(IsBuilt && solutionPaths.Contains(project.ToString()))
                        .SetNoRestore(IsRestored && solutionPaths.Contains(project.ToString()))
                        .SetOutputDirectory(output));
                }

                var packages = NuGetPackageTasks.OrderForPublish(NuGetPackageTasks.FindPackages(PackagesDirectory));
                if (packages.Count == 0) throw new InvalidOperationException("No NuGet packages were produced.");
                Log.Information("Packed {Count} packages into {Directory}", packages.Count, PackagesDirectory);
            });

        Target Publish => d => d
            .Description("Publishes NuGet packages after their packaged dependencies")
            .DependsOn(Pack)
            .Requires(() => NuGetSource)
            .Executes(() => {
                var packages = NuGetPackageTasks.OrderForPublish(NuGetPackageTasks.FindPackages(PackagesDirectory));
                foreach (var package in packages)
                {
                    Log.Information("Pushing {Package} to {Source}", Path.GetFileName(package), NuGetSource);
                    DotNetNuGetPush(s => s
                        .SetTargetPath(package)
                        .SetSource(NuGetSource)
                        .SetApiKey(NuGetApiKey)
                        .SetSkipDuplicate(SkipDuplicate));
                }
            });
    }
}
