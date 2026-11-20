using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Validation;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Creates a minimal solution and installs the packaged build scripts without replacing existing files.
/// </summary>
public static class SolutionScaffolder
{
    // These files are never replaced. Text settings and global.json's test runner may be added, but only on request.
    private static readonly string[] Preserved = [".gitignore", ".editorconfig", "global.json", "nuget.config", "Directory.Build.props"];

    /// <summary>
    /// Installs the selected packaged files into <paramref name="root"/> and, unless excluded, creates
    /// <c>src/Core/Core.csproj</c>, the <c>tests</c>, <c>samples</c> and <c>docs</c> folders, a
    /// <c>Directory.Build.props</c>, and a solution named <paramref name="solutionName"/> recorded in Fallout's
    /// <c>.fallout/parameters.json</c> (the engine's parameters; optional toolkit preferences stay at the solution root).
    /// Existing <c>.gitignore</c>, <c>.editorconfig</c>, <c>global.json</c>,
    /// <c>nuget.config</c> and <c>Directory.Build.props</c> files are kept. Scaffolding does not create preferences.
    /// </summary>
    /// <remarks>
    /// The packaged files are embedded resources (the Makefile, <c>scripts</c>, settings files, ...). Creating the
    /// solution requires <c>build/_build.csproj</c> (see <see cref="BuildHostScaffolder"/>). Nothing is written when a
    /// conflict is detected.
    /// </remarks>
    /// <param name="root">The directory to install into.</param>
    /// <param name="solutionName">The solution name; required only when the <c>solution</c> item is selected.</param>
    /// <param name="options">Which items to install, and how to treat existing settings files.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when the solution is selected and <paramref name="solutionName"/> is invalid, or a selector is unknown.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when generated files or a solution already exist, the build host is missing, or Fallout's parameters
    /// already define a solution.
    /// </exception>
    public static void Create(string root, string? solutionName, ScaffoldOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        root = Path.GetFullPath(root);
        var log = options.Log ?? (_ => { });
        var assembly = typeof(SolutionScaffolder).Assembly;
        var resources = BoilerplateCatalog.Resources().ToArray();
        var required = new[] { "Makefile", "scripts/build.anarchy.sh", ".gitignore", ".editorconfig", "global.json", "nuget.config" };
        if (required.Any(path => resources.All(resource => resource.Path != path)))
            throw new InvalidOperationException("The packaged boilerplate files are missing.");

        var selection = new ScaffoldSelection(resources.Select(resource => resource.Path), options);
        var selectedResources = resources.Where(resource => selection.Includes(resource.Path)).ToArray();
        var createSolution = selection.Includes("solution");
        var createCore = selection.Includes("core");
        var createProps = selection.Includes("Directory.Build.props");
        var directories = new[] { "tests", "samples", "docs" }.Where(selection.Includes).ToArray();
        if (createSolution && !NameRules.IsValidSolutionName(solutionName))
            throw new ArgumentException("Supply --boilerplate-solution-name using letters, digits, dots, underscores or hyphens.");

        var skillDirectories = options.SkillDirectories ?? [AgentSkillTasks.DefaultDirectory];
        if (skillDirectories.Count == 0 && selectedResources.Any(resource => IsPackagedSkill(resource.Path)))
            throw new InvalidOperationException("Agent-skill installation needs at least one agentSkills.directories entry.");
        var generated = selectedResources.Select(resource => resource.Path)
            .Where(path => !IsPreserved(path))
            .Select(path => DestinationPath(path, skillDirectories))
            .Concat(createCore ? ["src/Core/Core.csproj"] : [])
            .Concat(directories.Select(directory => directory + "/.gitkeep"));
        var conflicts = generated.Select(path => Path.Combine(root, path))
            .Where(path => File.Exists(path) || Directory.Exists(path)).ToList();
        conflicts.AddRange(selectedResources.Select(resource => resource.Path)
            .Where(IsPreserved)
            .Concat(createProps ? ["Directory.Build.props"] : [])
            .Select(path => FileHelper.ExistingEntryPath(root, path))
            .Where(Directory.Exists));
        if (createSolution)
            conflicts.AddRange(Directory.EnumerateFiles(root, "*.sln*", SearchOption.TopDirectoryOnly));
        if (options.DryRun)
        {
            ReportPlan(root, solutionName, options, selectedResources.Select(resource => resource.Path), conflicts,
                createCore, createProps, createSolution, directories, skillDirectories, log);
            return;
        }
        if (conflicts.Count > 0)
            throw new InvalidOperationException("Boilerplate would overwrite existing files: " + string.Join(", ", conflicts));

        var parametersPath = BuildEngineParameters.PathOf(root);
        var parameters = createSolution ? ReadParameters(root, parametersPath) : null;

        foreach (var resource in selectedResources)
        {
            var destinationPath = DestinationPath(resource.Path, skillDirectories);
            var destination = IsPreserved(resource.Path)
                ? FileHelper.ExistingEntryPath(root, resource.Path)
                : Path.Combine(root, destinationPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var input = assembly.GetManifestResourceStream(resource.Name)!;
            if (File.Exists(destination))
            {
                if (options.AppendExisting && resource.Path is ".gitignore" or ".editorconfig")
                {
                    using var reader = new StreamReader(input);
                    var existing = File.ReadAllText(destination);
                    var merged = FileHelper.MergeManagedBlock(existing, reader.ReadToEnd(),
                        skipExistingLines: resource.Path == ".gitignore",
                        placeBlockFirst: resource.Path is ".gitignore" or ".editorconfig");
                    if (merged == existing)
                    {
                        log($"{destination} already has the packaged settings");
                        continue;
                    }
                    File.WriteAllText(destination, merged);
                    log($"Applied packaged settings to {destination}");
                    if (resource.Path == ".gitignore") WarnAboutIgnoredTrackedFiles(root, log);
                }
                else if (options.AppendExisting && resource.Path == "global.json" &&
                         FileHelper.AddMissingJsonProperty(destination, input, "test"))
                {
                    log($"Added the packaged test runner to {destination}");
                }
                else
                {
                    log($"Preserving existing {destination}");
                }
                continue;
            }

            using (var output = new FileStream(destination, FileMode.CreateNew)) input.CopyTo(output);
            if (resource.Path == ".gitignore") WarnAboutIgnoredTrackedFiles(root, log);
            if (!OperatingSystem.IsWindows() && destination.EndsWith(".sh", StringComparison.Ordinal))
                File.SetUnixFileMode(destination, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        if (createCore)
            FileHelper.WriteNewText(Path.Combine(root, "src", "Core", "Core.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                </Project>

                """);
        if (createCore) ReportStarterLayoutHint(log);
        foreach (var directory in directories)
            FileHelper.WriteNewText(Path.Combine(root, directory, ".gitkeep"), "");

        var propsPath = FileHelper.ExistingEntryPath(root, "Directory.Build.props");
        if (createProps && !File.Exists(propsPath))
            FileHelper.WriteNewText(propsPath, "<Project>\n  <ItemGroup>\n    <EmbeddedResource Include=\"**\\Queries\\**\\*.sql\" />\n  </ItemGroup>\n</Project>\n");
        else if (createProps)
            log($"Preserving existing {propsPath}");

        if (createSolution)
        {
            DotNetCli.Run(root, "new", "sln", "--format", "sln", "--name", solutionName!);
            DotNetCli.Run(root, ["sln", solutionName + ".sln", "add", "build/_build.csproj", .. createCore ? new[] { "src/Core/Core.csproj" } : []]);
            Directory.CreateDirectory(Path.GetDirectoryName(parametersPath)!);
            parameters!["Solution"] = solutionName + ".sln";
            File.WriteAllText(parametersPath, parameters.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
    }

    private static void ReportPlan(string root, string? solutionName, ScaffoldOptions options, IEnumerable<string> paths,
        IReadOnlyCollection<string> conflicts, bool createCore, bool createProps, bool createSolution, string[] directories,
        IReadOnlyList<string> skillDirectories, Action<string> log)
    {
        log("Dry run: nothing is written.");
        foreach (var conflict in conflicts)
            log($"Conflict: {Path.GetRelativePath(root, conflict)} already exists; the install would stop without writing anything");
        foreach (var path in paths)
        {
            var destinationPath = DestinationPath(path, skillDirectories);
            var existing = IsPreserved(path) ? FileHelper.ExistingEntryPath(root, path) : Path.Combine(root, destinationPath);
            if (!File.Exists(existing))
            {
                if (!Directory.Exists(existing)) log($"Create: {destinationPath}");
                continue;
            }
            if (!IsPreserved(path)) continue;
            var packaged = BoilerplateCatalog.Read(path)!;
            if (options.AppendExisting && path is ".gitignore" or ".editorconfig")
            {
                var current = File.ReadAllText(existing);
                var merged = FileHelper.MergeManagedBlock(current, packaged,
                    skipExistingLines: path == ".gitignore", placeBlockFirst: path is ".gitignore" or ".editorconfig");
                log(merged == current ? $"Up to date: {path}" : $"Update the managed block of {path}:\n{UnifiedDiff.Create(path, current, merged)}");
            }
            else if (options.AppendExisting && path == "global.json")
                log($"Add the packaged test runner to {path} unless it has one");
            else
                log($"Keep: {path}");
        }
        if (createCore) log(File.Exists(Path.Combine(root, "src", "Core", "Core.csproj")) ? "Keep: src/Core/Core.csproj" : "Create: src/Core/Core.csproj");
        if (createCore) ReportStarterLayoutHint(log);
        foreach (var directory in directories) log($"Create: {directory}/.gitkeep");
        if (createProps) log(File.Exists(FileHelper.ExistingEntryPath(root, "Directory.Build.props")) ? "Keep: Directory.Build.props" : "Create: Directory.Build.props");
        if (createSolution) log($"Create: {solutionName}.sln, recorded in .fallout/parameters.json");
    }

    private static bool IsPreserved(string path) => Preserved.Contains(path, StringComparer.OrdinalIgnoreCase);

    private static bool IsPackagedSkill(string path) => path.StartsWith(".agents/skills/", StringComparison.Ordinal);

    private static void ReportStarterLayoutHint(Action<string> log) =>
        log("Without saving preferences, repeat this starter layout override: anarchy-toolkit scaffold api NewApi --layering-library-root src; make anarchy TARGET='CheckLayering --LayeringLibraryRoot src'.");

    private static string DestinationPath(string path, IReadOnlyList<string> skillDirectories)
    {
        const string packagedSkills = ".agents/skills/";
        if (!IsPackagedSkill(path)) return path;
        if (skillDirectories.Count == 0)
            throw new InvalidOperationException("Agent-skill installation needs at least one agentSkills.directories entry.");
        var directory = AgentSkillTasks.NormalizeDirectory(skillDirectories[0]);
        return directory + "/" + path[packagedSkills.Length..];
    }

    /// <summary>
    /// Lists the files Git tracks although the <c>.gitignore</c> files now ignore them: Git keeps tracking them, but
    /// new files like them would go unnoticed. Empty outside a Git work tree or without Git.
    /// </summary>
    /// <param name="root">The directory to check.</param>
    /// <returns>The tracked files that are ignored, relative to <paramref name="root"/>.</returns>
    public static IReadOnlyList<string> FindIgnoredTrackedFiles(string root)
    {
        try
        {
            var result = LocalProcessTasks.Run(root, "git", ["ls-files", "--cached", "--ignored", "--exclude-standard"]);
            return result.ExitCode == 0
                ? result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : [];
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return [];
        }
    }

    private static void WarnAboutIgnoredTrackedFiles(string root, Action<string> log)
    {
        foreach (var file in FindIgnoredTrackedFiles(root))
            log($"Warning: '{file}' is tracked but now matches .gitignore; untrack it (git rm --cached) or add '!{file}' below the managed block");
    }

    private static JsonObject ReadParameters(string root, string parametersPath)
    {
        if (!File.Exists(Path.Combine(root, "build", "_build.csproj")))
            throw new InvalidOperationException("Create build/_build.csproj before scaffolding a solution; see the Automation.Build README.");
        var parameters = File.Exists(parametersPath)
            ? JsonNode.Parse(File.ReadAllText(parametersPath)) as JsonObject
              ?? throw new InvalidOperationException("Fallout parameters.json must contain a JSON object.")
            : new JsonObject();
        if (parameters.Any(property => string.Equals(property.Key, "Solution", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Fallout parameters.json already defines a solution; it will not be overwritten.");
        return parameters;
    }
}
