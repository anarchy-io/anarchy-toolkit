using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Solutions;
using Serilog;
using System.Xml.Linq;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    public partial interface IStandardBuild
    {
        Target ListProjects => d => d
            .Description("Lists all valid projects within the solution")
            .Executes(() => {
                foreach (var project in Solution.AllProjects)
                {
                    var relativePath = RelativePath(project);

                    Log.Information("{Icon} {ProjectName} ({RelativePath})", GetProjectIcon(project, relativePath),
                        project.Name, relativePath);
                }
            });

        /// <summary>The kinds of test projects run by the <c>Test*</c> targets.</summary>
        static class TestKind
        {
            public const string Unit = nameof(Unit);
            public const string Integration = nameof(Integration);
            public const string EndToEnd = nameof(EndToEnd);
        }

        string GetProjectIcon(Project project, string relativePath)
        {
            if (IsTestProject(project) || relativePath.StartsWith("tests"))
            {
                return "🧪";
            }

            if (relativePath.StartsWith("build"))
            {
                return "🔧";
            }

            return IsRunnableProject(project.Path) ? "🚀" : "📦";
        }

        /// <summary>
        /// Runs every test project of the given kind (see <see cref="IsTestProjectOfKind"/>), skipping the build
        /// when <see cref="Compile"/> already ran.
        /// </summary>
        void RunTestsOfKind(string kind)
        {
            var projects = Solution.AllProjects
                .Where(p => IsTestProject(p) && IsTestProjectOfKind(p, kind))
                .OrderBy(p => p.Name)
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
        /// <c>EndToEnd</c>). By default, see <see cref="MatchesTestKind"/>.
        /// </summary>
        bool IsTestProjectOfKind(Project project, string kind) => MatchesTestKind(project.Name, kind);

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

        IReadOnlyCollection<Project> GetRunnableProjects() => Solution.AllProjects
            .Where(project => IsRunnableProject(project.Path))
            .OrderBy(project => project.Name)
            .ToArray();

        /// <summary>Whether the project lives under a <c>src</c> directory.</summary>
        static bool IsInSourceDirectory(AbsolutePath projectPath)
        {
            var path = projectPath.ToString().Replace('\\', '/');

            return path.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/src/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Whether the project lives under a <c>src</c> directory and has <c>OutputType</c> <c>Exe</c>.</summary>
        static bool IsRunnableProject(AbsolutePath projectPath)
        {
            if (!IsInSourceDirectory(projectPath))
            {
                return false;
            }

            var outputType = XDocument.Load(projectPath)
                .Descendants()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, "OutputType", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            return string.Equals(outputType, "Exe", StringComparison.OrdinalIgnoreCase);
        }

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

        IReadOnlyCollection<Project> ResolveRunnableProjectsOrFail(IEnumerable<string> appNames) =>
            appNames
                .Select(ResolveRunnableProjectOrFail)
                .DistinctBy(p => p.Path)
                .ToArray();

        void LogRunnableApps(IReadOnlyCollection<Project>? runnableProjects = null)
        {
            var projects = runnableProjects ?? GetRunnableProjects();
            if (projects.Count == 0)
            {
                Log.Warning("No runnable apps were found in src/**.csproj with OutputType=Exe");
                return;
            }

            foreach (var project in projects)
            {
                Log.Warning("- {AppName} ({RelativePath})", project.Name, RelativePath(project));
            }
        }

        static IReadOnlyCollection<string> ParseAppNames(string raw) =>
            raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
