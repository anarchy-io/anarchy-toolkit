using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Core.Template;
using Anarchy.Toolkit.Automation.Core.Validation;
using Anarchy.Toolkit.Automation.Helpers.DotNet;
using Anarchy.Toolkit.Automation.Helpers.Files;
using Anarchy.Toolkit.Automation.Helpers.Packages;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Build.Cli.Service;

/// <summary>Plans and creates the shared test project for a kind recognized by the Anarchy build.</summary>
public static class TestProjectScaffolder
{
    /// <summary>The canonical test kinds accepted by the standard Anarchy build.</summary>
    public static IReadOnlyList<string> Kinds => SolutionPlan.AllTestKinds;

    /// <summary>Resolves the configured test root and first matching <c>TestKindAliases</c> value.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the kind, aliases or test root are invalid, or the project already exists.</exception>
    public static TestProjectPlan Plan(string root, string kind, JsonObject parameters, AutomationPreferences preferences)
    {
        var canonicalKind = Kinds.FirstOrDefault(value => string.Equals(value, kind, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException($"Test kind must be {string.Join(", ", Kinds)}.");
        var aliases = ReadAliases(parameters, preferences);
        var configuredAlias = aliases.Where(entry => string.Equals(entry.Kind, canonicalKind, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Alias).FirstOrDefault();
        var projectName = configuredAlias ?? canonicalKind;
        if (!NameRules.IsValidSolutionName(projectName))
            throw new InvalidOperationException($"TestKindAliases contains '{projectName}', which is not a valid project-name segment.");

        var testsRoot = GetString(parameters, "TestsRoot") ?? preferences.Get<string>("build.testsRoot") ?? "tests";
        var normalizedRoot = testsRoot.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(normalizedRoot) || Path.IsPathRooted(normalizedRoot) ||
            normalizedRoot.Split('/').Any(segment => segment is ".." or "."))
            throw new InvalidOperationException($"TestsRoot '{testsRoot}' must be a non-empty path inside the solution.");

        var relativeDirectory = $"{normalizedRoot.TrimEnd('/')}/{projectName}";
        var relativeProject = $"{relativeDirectory}/{projectName}.csproj";
        var directory = Path.GetFullPath(relativeDirectory.Replace('/', Path.DirectorySeparatorChar), root);
        if (!IsWithin(root, directory))
            throw new InvalidOperationException($"TestsRoot '{testsRoot}' must be inside the solution.");
        if (Directory.Exists(directory) || File.Exists(directory) || File.Exists(Path.Combine(directory, projectName + ".csproj")))
            throw new InvalidOperationException($"{relativeDirectory} already exists.");

        var existingProject = Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
                .FirstOrDefault(path => string.Equals(Path.GetFileNameWithoutExtension(path), projectName, StringComparison.OrdinalIgnoreCase))
            : null;
        if (existingProject is not null)
            throw new InvalidOperationException($"A project named {projectName} already exists at {Path.GetRelativePath(root, existingProject)}.");

        var commonName = preferences.Get<string>("tests.commonProject") ?? SolutionPlan.DefaultCommonTestProject;
        if (!NameRules.IsValidSolutionName(commonName) || string.Equals(commonName, projectName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"tests.commonProject '{commonName}' cannot name the common test project.");
        var commonDirectory = $"{normalizedRoot.TrimEnd('/')}/{commonName}";
        return new TestProjectPlan(canonicalKind, projectName, relativeDirectory, relativeProject, directory) {
            CommonProjectName = commonName,
            RelativeCommonProjectPath = $"{commonDirectory}/{commonName}.csproj",
            CommonExists = File.Exists(Path.Combine(root, commonDirectory, commonName + ".csproj"))
        };
    }

    /// <summary>
    /// Creates a test project referencing the common test project, creating that one first when it is missing; the
    /// common project references the toolkit's test kits (see <see cref="CommonPackages"/>).
    /// </summary>
    public static void Add(string root, string solutionFile, TestProjectPlan plan, bool dryRun, ToolkitPackageVersions versions,
        Action<string>? progress = null)
    {
        var report = progress ?? (_ => { });
        var projects = plan.CommonExists ? new[] { plan.RelativeProjectPath } : [plan.RelativeCommonProjectPath, plan.RelativeProjectPath];
        if (dryRun)
        {
            report("Dry run: nothing is written.");
            foreach (var project in projects) report($"Create: {project}");
            report($"Add {string.Join(" and ", projects)} to {Path.GetFileName(solutionFile)}");
            return;
        }

        var solutionName = Path.GetFileNameWithoutExtension(solutionFile);
        var commonDirectory = Path.GetDirectoryName(Path.Combine(root, plan.RelativeCommonProjectPath))!;
        if (!plan.CommonExists)
        {
            report($"Creating {plan.RelativeCommonProjectPath}");
            var central = CentralPackages.Find(commonDirectory);
            var packages = CommonPackages(root).Select(id => {
                if (central is not null) CentralPackages.Ensure(central, id, versions[id]);
                return (id, central is null ? versions[id] : null);
            }).ToArray();
            foreach (var (file, content) in TestTemplates.Common(plan.CommonProjectName,
                         SolutionLayout.Namespace(solutionName, Path.GetDirectoryName(plan.RelativeCommonProjectPath)!), packages))
                FileHelper.WriteNewText(Path.Combine(commonDirectory, file), content);
        }

        report($"Creating {plan.RelativeProjectPath}");
        FileHelper.WriteNewText(Path.Combine(plan.Directory, plan.ProjectName + ".csproj"), TestTemplates.TestProject(
            SolutionLayout.Namespace(solutionName, plan.RelativeDirectory),
            Path.GetRelativePath(plan.Directory, Path.Combine(root, plan.RelativeCommonProjectPath)).Replace('\\', '/')));
        report($"Adding it to {Path.GetFileName(solutionFile)}");
        DotNetCli.Run(root, ["sln", solutionFile, "add", .. projects]);
    }

    /// <summary>
    /// The test kits a new common project references: <see cref="TestTemplates.TestingPackage"/>, plus
    /// <see cref="TestTemplates.TestingWebPackage"/> when a project references a web package and
    /// <see cref="TestTemplates.TestingMockServerPackage"/> when a project's name ends in <c>MockServer</c>.
    /// </summary>
    public static IReadOnlyList<string> CommonPackages(string root)
    {
        var projects = Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
                .ToArray()
            : [];
        var packages = new List<string> { TestTemplates.TestingPackage };
        if (projects.Any(path => File.ReadAllText(path).Contains("\"Anarchy.Toolkit.Runtime.Web", StringComparison.Ordinal)))
            packages.Add(TestTemplates.TestingWebPackage);
        if (projects.Any(path => Path.GetFileNameWithoutExtension(path).EndsWith(TestTemplates.MockServerProject, StringComparison.Ordinal)))
            packages.Add(TestTemplates.TestingMockServerPackage);
        return packages;
    }

    private static IReadOnlyList<(string Kind, string Alias)> ReadAliases(JsonObject parameters, AutomationPreferences preferences)
    {
        var configured = Find(parameters, "TestKindAliases");
        var entries = configured is null
            ? preferences.Get<string[]>("tests.kindAliases") ?? []
            : configured is JsonArray array
                ? array.Select(value => value?.GetValue<string>() ?? throw new InvalidOperationException("TestKindAliases entries must be strings.")).ToArray()
                : throw new InvalidOperationException("TestKindAliases must be an array of Kind=Alias strings.");

        return entries.Select(entry => {
            var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts is not [{ Length: > 0 } kind, { Length: > 0 } alias])
                throw new InvalidOperationException($"TestKindAliases entries must be Kind=Alias, e.g. EndToEnd=Acceptance; got '{entry}'.");
            return (kind, alias);
        }).ToArray();
    }

    private static string? GetString(JsonObject parameters, string key) =>
        Find(parameters, key) is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static JsonNode? Find(JsonObject parameters, string key) =>
        parameters.FirstOrDefault(property => string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase)).Value;

    private static bool IsWithin(string root, string path)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(path));
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !Path.IsPathRooted(relative);
    }
}

/// <summary>The resolved output paths and project name for a test kind.</summary>
/// <param name="Kind">The canonical test kind.</param>
/// <param name="ProjectName">The canonical kind or its configured alias.</param>
/// <param name="RelativeDirectory">The solution-relative test project directory.</param>
/// <param name="RelativeProjectPath">The solution-relative test project file.</param>
/// <param name="Directory">The absolute test project directory.</param>
public sealed record TestProjectPlan(
    string Kind,
    string ProjectName,
    string RelativeDirectory,
    string RelativeProjectPath,
    string Directory)
{
    /// <summary>The name of the common test project (<c>tests.commonProject</c>).</summary>
    public string CommonProjectName { get; init; } = SolutionPlan.DefaultCommonTestProject;

    /// <summary>The solution-relative common test project file.</summary>
    public string RelativeCommonProjectPath { get; init; } = $"tests/{SolutionPlan.DefaultCommonTestProject}/{SolutionPlan.DefaultCommonTestProject}.csproj";

    /// <summary>Whether the common test project already exists, so only the test project is created.</summary>
    public bool CommonExists { get; init; }
}
