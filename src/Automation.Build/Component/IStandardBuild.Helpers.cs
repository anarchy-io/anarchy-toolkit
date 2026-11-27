using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Build.Helpers.Consoles;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Solutions;
using Serilog;
using Spectre.Console;
using System.Xml.Linq;

namespace Anarchy.Toolkit.Automation.Build.Component;

public partial interface IStandardBuild
{
    /// <summary>
    /// Lists the solution's projects with their path, marking test, build, runnable and library projects.
    /// </summary>
    Target ListProjects => d => d
        .Description("Lists all valid projects within the solution")
        .Executes(() => {
            var table = BuildConsole.Table("", "Project", "Path");
            foreach (var project in Solution.AllProjects.OrderBy(p => RelativePath(p), StringComparer.Ordinal))
            {
                var relativePath = RelativePath(project);
                table.AddRow(GetProjectIcon(project, relativePath), Markup.Escape(project.Name), $"[grey]{Markup.Escape(relativePath)}[/]");
            }

            BuildConsole.Write(table, ToolVerbosity);
        });

    /// <summary>
    /// The kinds of test projects run by the <c>Test*</c> targets.
    /// </summary>
    static class TestKind
    {
        /// <summary>
        /// Unit tests.
        /// </summary>
        public const string Unit = nameof(Unit);
        /// <summary>
        /// Integration tests.
        /// </summary>
        public const string Integration = nameof(Integration);
        /// <summary>
        /// End-to-end tests.
        /// </summary>
        public const string EndToEnd = nameof(EndToEnd);
    }

    /// <summary>
    /// Returns the icon <see cref="ListProjects"/> shows for a project: test, build, runnable app or library.
    /// </summary>
    /// <param name="project">The solution project.</param>
    /// <param name="relativePath">The project's path relative to the solution (see <see cref="IStandardBuild.RelativePath(Project)"/>).</param>
    /// <returns>An emoji.</returns>
    string GetProjectIcon(Project project, string relativePath)
    {
        if (IsTestProject(project) || IsInDirectory(project.Path, TestsDirectory))
        {
            return "🧪";
        }

        if (relativePath.StartsWith("build"))
        {
            return "🔧";
        }

        return IsRunnableProject(project.Path, SourceDirectory) ? "🚀" : "📦";
    }

    /// <summary>
    /// Runs every test project of the given kind (see <see cref="IsTestProjectOfKind"/>), skipping the build
    /// when <see cref="Compile"/> already ran.
    /// </summary>
    /// <param name="kind">The kind of tests, one of the <see cref="TestKind"/> constants.</param>
    void RunTestsOfKind(string kind)
    {
        var projects = Solution.AllProjects
            .Where(p => IsTestProject(p) && IsTestProjectOfKind(p, kind))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        if (projects.Length == 0)
        {
            Log.Warning("No '{Kind}' test projects found. Skipping", kind);
            return;
        }

        Log.Information("Testing {ProjectNames}", projects.Select(p => p.Name).Join(", "));

        this.RunTests(projects.Select(p => p.Path), noBuild: IsBuilt);
    }

    /// <summary>
    /// Whether the test project holds tests of the given kind (<c>Unit</c>, <c>Integration</c> or
    /// <c>EndToEnd</c>): by default when <see cref="MatchesTestKind"/> matches the kind or one of its
    /// <see cref="TestKindAliases"/>.
    /// </summary>
    /// <param name="project">The test project.</param>
    /// <param name="kind">The kind of tests, one of the <see cref="TestKind"/> constants.</param>
    /// <returns><see langword="true"/> if the project holds tests of that kind; otherwise <see langword="false"/>.</returns>
    bool IsTestProjectOfKind(Project project, string kind) =>
        MatchesTestKind(project.Name, kind) ||
        (ParseTestKindAliases(TestKindAliases).TryGetValue(kind, out var aliases) && aliases.Any(alias => MatchesTestKind(project.Name, alias)));

    /// <summary>
    /// Whether a project name denotes tests of the given kind: either it is the kind itself (a single
    /// <c>Unit</c> project) or one of its dot-separated segments is the kind and another one is <c>Tests</c>
    /// (<c>Tests.Unit.Core</c>, <c>MyService.Unit.Tests</c>, <c>MyService.Tests.Unit</c>).
    /// </summary>
    internal static bool MatchesTestKind(string projectName, string kind)
    {
        if (string.Equals(projectName, kind, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var segments = projectName.Split('.');

        return segments.Contains(kind, StringComparer.OrdinalIgnoreCase)
               && segments.Contains("Tests", StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The solution's runnable projects (see <see cref="IsRunnableProject"/>), ordered by name.
    /// </summary>
    /// <returns>The runnable projects.</returns>
    IReadOnlyCollection<Project> GetRunnableProjects() => Solution.AllProjects
        .Where(project => IsRunnableProject(project.Path, SourceDirectory))
        .OrderBy(project => project.Name)
        .ToArray();

    /// <summary>
    /// Whether the project lives under a <c>src</c> directory.
    /// </summary>
    /// <param name="projectPath">The project file.</param>
    /// <returns><see langword="true"/> if a segment of the path is <c>src</c>; otherwise <see langword="false"/>.</returns>
    static bool IsInSourceDirectory(AbsolutePath projectPath)
    {
        var path = projectPath.ToString().Replace('\\', '/');

        return path.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/src/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether the project lives under <paramref name="sourceDirectory"/> (by default, any <c>src</c> directory) and
    /// has <c>OutputType</c> <c>Exe</c>.
    /// </summary>
    /// <param name="projectPath">The project file, read as XML.</param>
    /// <param name="sourceDirectory">The production code directory; <see langword="null"/> accepts any <c>src</c> directory.</param>
    /// <returns><see langword="true"/> if the project is runnable; otherwise <see langword="false"/>.</returns>
    static bool IsRunnableProject(AbsolutePath projectPath, AbsolutePath? sourceDirectory = null)
    {
        if (sourceDirectory is null ? !IsInSourceDirectory(projectPath) : !IsInDirectory(projectPath, sourceDirectory))
        {
            return false;
        }

        var outputType = XDocument.Load(projectPath)
            .Descendants()
            .FirstOrDefault(e => string.Equals(e.Name.LocalName, "OutputType", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return string.Equals(outputType, "Exe", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Finds the runnable project named <paramref name="appName"/> (case-insensitive), logging the available
    /// apps when there is none.
    /// </summary>
    /// <param name="appName">The app name.</param>
    /// <returns>The runnable project.</returns>
    /// <exception cref="Exception">Thrown when no runnable project has that name.</exception>
    Project ResolveRunnableProjectOrFail(string appName)
    {
        var runnableProjects = GetRunnableProjects();
        var project = runnableProjects.SingleOrDefault(p => string.Equals(p.Name, appName, StringComparison.OrdinalIgnoreCase));

        if (project is not null)
        {
            return project;
        }

        Log.Error("App '{AppName}' is not runnable. Available options:", appName);
        LogRunnableApps(runnableProjects);

        throw new Exception($"Unknown app '{appName}'. Available options: {runnableProjects.Select(p => p.Name).Join(", ")}");
    }

    /// <summary>
    /// Finds the runnable projects named <paramref name="appNames"/> (see <see cref="ResolveRunnableProjectOrFail"/>),
    /// without duplicates.
    /// </summary>
    /// <param name="appNames">The app names, case-insensitive.</param>
    /// <returns>The matching projects.</returns>
    /// <exception cref="Exception">Thrown when any name matches no runnable project.</exception>
    IReadOnlyCollection<Project> ResolveRunnableProjectsOrFail(IEnumerable<string> appNames) =>
        appNames
            .Select(ResolveRunnableProjectOrFail)
            .DistinctBy(p => p.Path)
            .ToArray();

    /// <summary>
    /// Shows the runnable apps in a table, or warns when there are none.
    /// </summary>
    /// <param name="runnableProjects">The apps to show, or <see langword="null"/> for <see cref="GetRunnableProjects"/>.</param>
    void LogRunnableApps(IReadOnlyCollection<Project>? runnableProjects = null)
    {
        var projects = runnableProjects ?? GetRunnableProjects();
        if (projects.Count == 0)
        {
            Log.Warning("No runnable apps with OutputType=Exe were found under {SourceDirectory}", SourceDirectory);
            return;
        }

        var table = BuildConsole.Table("App", "Path");
        foreach (var project in projects)
        {
            table.AddRow($"[green]{Markup.Escape(project.Name)}[/]", $"[grey]{Markup.Escape(RelativePath(project))}[/]");
        }

        BuildConsole.Write(table, ToolVerbosity);
    }

    /// <summary>
    /// Splits a comma-separated list of app names, trimming them and dropping empty and duplicate ones.
    /// </summary>
    /// <param name="raw">The comma-separated names.</param>
    /// <returns>The distinct names, compared case-insensitively.</returns>
    static IReadOnlyCollection<string> ParseAppNames(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
