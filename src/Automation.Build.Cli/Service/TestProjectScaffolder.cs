using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Core.Validation;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Build.Cli.Service;

/// <summary>Plans and creates the shared test project for a kind recognized by the Anarchy build.</summary>
public static class TestProjectScaffolder
{
    /// <summary>The canonical test kinds accepted by the standard Anarchy build.</summary>
    public static IReadOnlyList<string> Kinds { get; } = ["Unit", "Integration", "EndToEnd"];

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

        return new TestProjectPlan(canonicalKind, projectName, relativeDirectory, relativeProject, directory);
    }

    /// <summary>Creates a test project that inherits shared settings from its test-root <c>Directory.Build.props</c>.</summary>
    public static void Add(string root, string solutionFile, TestProjectPlan plan, bool dryRun, Action<string>? progress = null)
    {
        var report = progress ?? (_ => { });
        if (dryRun)
        {
            report("Dry run: nothing is written.");
            report($"Create: {plan.RelativeProjectPath}");
            report($"Add {plan.RelativeProjectPath} to {Path.GetFileName(solutionFile)}");
            return;
        }

        report($"Creating {plan.RelativeProjectPath}");
        FileHelper.WriteNewText(Path.Combine(plan.Directory, plan.ProjectName + ".csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>

            """);
        report($"Adding it to {Path.GetFileName(solutionFile)}");
        DotNetCli.Run(root, "sln", solutionFile, "add", plan.RelativeProjectPath);
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
    string Directory);
