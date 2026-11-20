using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Build.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;

namespace Anarchy.Toolkit.Automation.Build.Component;

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
/// A build keeps the same shape: <c>partial class Build : AnarchyBuild, IStandardBuild</c>, with its own
/// <c>Build.*.cs</c> files for whatever is specific to the repository.
/// </para>
/// </summary>
/// <remarks>
/// Optional components built on it add more targets: <see cref="ILayeringBuild"/>, <see cref="INuGetBuild"/>,
/// <see cref="IContainerBuild"/>, <see cref="IApiReferenceBuild"/>, <see cref="IDependencyMapBuild"/> and
/// <see cref="IDependencyUpgradeBuild"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// partial class Build : AnarchyBuild, IStandardBuild, ILayeringBuild
/// {
///     public static int Main() => Execute&lt;Build&gt;(x => x.As&lt;IStandardBuild&gt;().Compile);
///
///     public IReadOnlyCollection&lt;Artifact&gt; Artifacts => [];
/// }
/// </code>
/// </example>
public partial interface IStandardBuild : IDefaultBuild
{
    /// <summary>
    /// Whether <see cref="LintFix"/> re-stages its fixes (<c>--auto-stage-lint-fixes</c>), only in files that were staged
    /// and had no unstaged changes before. Defaults to <see langword="false"/>; used by Git hooks.
    /// </summary>
    [Parameter("Re-stage lint fixes only in previously staged files without unstaged changes")]
    bool AutoStageLintFixes => TryGetValue<bool?>(() => AutoStageLintFixes) ?? false;

    /// <summary>
    /// Other names accepted for each test kind (<c>--test-kind-aliases</c>, or <c>TestKindAliases</c> in
    /// <c>.fallout/parameters.json</c>), as <c>Kind=Alias</c> entries such as <c>EndToEnd=Acceptance</c>; empty by default.
    /// See <see cref="IsTestProjectOfKind"/>.
    /// </summary>
    [Parameter("Other names for test kinds, as Kind=Alias (e.g. EndToEnd=Acceptance)")]
    string[] TestKindAliases => TryGetValue(() => TestKindAliases) ?? Preference<string[]>("tests.kindAliases") ?? [];

    /// <summary>
    /// Groups <c>Kind=Alias</c> entries by kind (case-insensitive).
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <returns>The aliases of each kind.</returns>
    /// <exception cref="ArgumentException">Thrown when an entry is not <c>Kind=Alias</c>.</exception>
    internal static IReadOnlyDictionary<string, string[]> ParseTestKindAliases(IEnumerable<string> entries) => entries
        .Select(entry => entry.Split('=', 2, StringSplitOptions.TrimEntries))
        .Select(parts => parts is [{ Length: > 0 } kind, { Length: > 0 } alias]
            ? (Kind: kind, Alias: alias)
            : throw new ArgumentException($"Test kind aliases are Kind=Alias, e.g. EndToEnd=Acceptance; got '{string.Join('=', parts)}'."))
        .GroupBy(pair => pair.Kind, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.Select(pair => pair.Alias).ToArray(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether <see cref="Restore"/> already ran, so later steps can skip restoring.
    /// </summary>
    bool IsRestored => FinishedTargets.Any(t => t.Name == nameof(Restore));

    /// <summary>
    /// Whether <see cref="Compile"/> already ran, so later steps can skip building.
    /// </summary>
    bool IsBuilt => FinishedTargets.Any(t => t.Name == nameof(Compile));

    /// <summary>
    /// Test projects live in <see cref="IDefaultBuild.TestsRoot"/>, else <c>tests</c> (plural), as in the toolkit.
    /// </summary>
    AbsolutePath IDefaultBuild.TestsDirectory => RootDirectory / (TestsRoot ?? "tests");

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
    /// <param name="project">The solution project.</param>
    /// <returns>The path, e.g. <c>src/Core/Core.csproj</c>.</returns>
    string RelativePath(Project project) =>
        Solution.Directory.GetRelativePathTo(project.Path).ToString().Replace('\\', '/');

    internal static bool IsInDirectory(AbsolutePath path, AbsolutePath directory) =>
        path.ToString().StartsWith(directory.ToString().TrimEnd('/', '\\') + Path.DirectorySeparatorChar, StringComparison.Ordinal);
}
