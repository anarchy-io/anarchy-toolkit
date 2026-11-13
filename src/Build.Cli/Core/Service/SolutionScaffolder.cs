using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Validation;
using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>Creates a minimal solution and installs the packaged build scripts without replacing existing files.</summary>
public static class SolutionScaffolder
{
    private const string ResourcePrefix = "Anarchy.Boilerplate/";

    // These files are never replaced. Text settings may be appended, but only on request.
    private static readonly string[] Preserved = [".gitignore", ".editorconfig", "global.json", "nuget.config", "Directory.Build.props"];

    public static void Create(string root, string? solutionName, ScaffoldOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        root = Path.GetFullPath(root);
        var log = options.Log ?? (_ => { });
        var assembly = typeof(SolutionScaffolder).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(name => (Name: name, Path: name[ResourcePrefix.Length..].Replace('\\', '/')))
            .ToArray();
        var required = new[] { "Makefile", "scripts/build.fallout.sh", ".gitignore", ".editorconfig", "global.json", "nuget.config" };
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

        var generated = selectedResources.Select(resource => resource.Path)
            .Where(path => !IsPreserved(path))
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
        if (conflicts.Count > 0)
            throw new InvalidOperationException("Boilerplate would overwrite existing files: " + string.Join(", ", conflicts));

        var parametersPath = Path.Combine(root, ".fallout", "parameters.json");
        var parameters = createSolution ? ReadParameters(root, parametersPath) : null;

        foreach (var resource in selectedResources)
        {
            var destination = IsPreserved(resource.Path)
                ? FileHelper.ExistingEntryPath(root, resource.Path)
                : Path.Combine(root, resource.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var input = assembly.GetManifestResourceStream(resource.Name)!;
            if (File.Exists(destination))
            {
                if (options.AppendExisting && resource.Path is ".gitignore" or ".editorconfig")
                {
                    FileHelper.AppendOnce(destination, input);
                    log($"Applied packaged settings to {destination}");
                }
                else
                {
                    log($"Preserving existing {destination}");
                }
                continue;
            }

            using (var output = new FileStream(destination, FileMode.CreateNew)) input.CopyTo(output);
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

    private static bool IsPreserved(string path) => Preserved.Contains(path, StringComparer.OrdinalIgnoreCase);

    private static JsonObject ReadParameters(string root, string parametersPath)
    {
        if (!File.Exists(Path.Combine(root, "build", "_build.csproj")))
            throw new InvalidOperationException("Create build/_build.csproj before scaffolding a solution; see the Build.Fallout README.");
        var parameters = File.Exists(parametersPath)
            ? JsonNode.Parse(File.ReadAllText(parametersPath)) as JsonObject
              ?? throw new InvalidOperationException("Fallout parameters.json must contain a JSON object.")
            : new JsonObject();
        if (parameters.Any(property => string.Equals(property.Key, "Solution", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Fallout parameters.json already defines a solution; it will not be overwritten.");
        return parameters;
    }
}
