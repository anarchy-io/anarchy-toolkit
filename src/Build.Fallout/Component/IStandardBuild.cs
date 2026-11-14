using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// <para>
/// The standard build, on top of <see cref="IDefaultBuild"/>: an incremental <c>Restore</c> →
/// <c>Compile</c> → <c>Test*</c> chain, <c>Lint</c>/<c>LintFix</c>, project listing and running apps locally.
/// </para>
///
/// <para>
/// It is a partial type split by concern:
/// <list type="bullet">
///   <item><c>IStandardBuild.cs</c> — parameters, state and the conventions overridden from <see cref="IDefaultBuild"/>.</item>
///   <item><c>IStandardBuild.Targets.cs</c> — <c>Restore</c>, <c>Compile</c>, <c>Test*</c>, <c>Lint*</c> and <c>PullRequest</c>.</item>
///   <item><c>IStandardBuild.Helpers.cs</c> — <c>ListProjects</c> and project discovery.</item>
///   <item><c>IStandardBuild.Apps.cs</c> — <c>RunApp</c> and <c>RunApps</c>.</item>
///   <item><c>IStandardBuild.AppSettings.cs</c> — <c>GenerateAppSettings</c>.</item>
/// </list>
/// A build keeps the same shape: <c>partial class Build : FalloutBuild, IStandardBuild</c>, with its own
/// <c>Build.*.cs</c> files for whatever is specific to the repository.
/// </para>
/// </summary>
public partial interface IStandardBuild : IDefaultBuild
{
    [Parameter("Re-stage lint fixes only in previously staged files without unstaged changes")]
    bool AutoStageLintFixes => TryGetValue<bool?>(() => AutoStageLintFixes) ?? false;

    /// <summary>
    /// Whether <see cref="Restore"/> already ran, so later steps can skip restoring.
    /// </summary>
    bool IsRestored => FinishedTargets.Any(t => t.Name == nameof(Restore));

    /// <summary>
    /// Whether <see cref="Compile"/> already ran, so later steps can skip building.
    /// </summary>
    bool IsBuilt => FinishedTargets.Any(t => t.Name == nameof(Compile));

    /// <summary>
    /// Test projects live in <c>tests</c> (plural), as in the toolkit.
    /// </summary>
    AbsolutePath IDefaultBuild.TestsDirectory => RootDirectory / "tests";

    /// <summary>
    /// Like <see cref="IDefaultBuild.IsTestProject"/>, but ignores projects under
    /// <see cref="IDefaultBuild.SourceDirectory"/>: shared test kits living there reference the test packages
    /// too, yet hold no tests.
    /// </summary>
    bool IDefaultBuild.IsTestProject(Project project) =>
        !IsInDirectory(project.Path, SourceDirectory)
        && project.ContainsPackageReferenceToAny(TestTasks.DefaultTestPackages);

    /// <summary>
    /// Returns the path of the project relative to the solution, with forward slashes.
    /// </summary>
    string RelativePath(Project project) =>
        Solution.Directory.GetRelativePathTo(project.Path).ToString().Replace('\\', '/');

    internal static bool IsInDirectory(AbsolutePath path, AbsolutePath directory) =>
        path.ToString().StartsWith(directory.ToString().TrimEnd('/', '\\') + Path.DirectorySeparatorChar, StringComparison.Ordinal);
}
