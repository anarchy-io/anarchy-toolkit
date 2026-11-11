using Serilog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    /// <summary>Controls which scaffold items are installed and whether existing text settings are appended.</summary>
    public sealed class BoilerplateOptions
    {
        /// <summary>Comma-separated item names or script paths; null means all items.</summary>
        public string? Items { get; set; }
        /// <summary>Comma-separated item names or script paths to remove from the selection.</summary>
        public string? Exclude { get; set; }
        /// <summary>Append the packaged text to existing .gitignore and .editorconfig files.</summary>
        public bool AppendExisting { get; set; }
    }

    /// <summary>Creates a minimal solution and installs the packaged build scripts without replacing existing files.</summary>
    public static partial class BoilerplateTasks
    {
        private const string ResourcePrefix = "Anarchy.Boilerplate/";

        public static void Create(string root, string? solutionName) => Create(root, solutionName, new BoilerplateOptions());

        public static void Create(string root, string? solutionName, BoilerplateOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            root = Path.GetFullPath(root);
            var assembly = typeof(BoilerplateTasks).Assembly;
            var resources = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                .Select(name => (Name: name, Path: name[ResourcePrefix.Length..].Replace('\\', '/')))
                .ToArray();
            var required = new[] { "Makefile", "scripts/build.fallout.sh", ".gitignore", ".editorconfig", "global.json", "nuget.config" };
            if (required.Any(path => !resources.Any(resource => resource.Path == path)))
                throw new InvalidOperationException("The packaged boilerplate files are missing.");

            var available = resources.Select(resource => resource.Path)
                .Concat(["solution", "core", "tests", "samples", "docs", "Directory.Build.props"]).ToArray();
            var includes = ParseSelectors(options.Items, defaultAll: true);
            var excludes = ParseSelectors(options.Exclude, defaultAll: false);
            foreach (var selector in includes.Concat(excludes).Where(selector => selector is not ("all" or "none")))
            {
                if (!available.Any(item => Matches(item, selector)))
                    throw new ArgumentException($"Unknown boilerplate item '{selector}'.");
            }
            bool Selected(string item) => includes.Any(selector => Matches(item, selector)) && !excludes.Any(selector => Matches(item, selector));
            var selectedResources = resources.Where(resource => Selected(resource.Path)).ToArray();
            var createSolution = Selected("solution");
            var createCore = Selected("core");
            var createProps = Selected("Directory.Build.props");
            var directories = new[] { "tests", "samples", "docs" }.Where(Selected).ToArray();
            if (createSolution && (string.IsNullOrWhiteSpace(solutionName) || !SolutionNameRegex().IsMatch(solutionName)))
                throw new ArgumentException("Supply --boilerplate-solution-name using letters, digits, dots, underscores or hyphens.");

            // These files are never replaced. Text settings may be appended, but only on request.
            var preserve = new[] { ".gitignore", ".editorconfig", "global.json", "nuget.config", "Directory.Build.props" };
            var generated = selectedResources.Select(resource => resource.Path)
                .Where(path => !preserve.Contains(path, StringComparer.OrdinalIgnoreCase))
                .Concat(createCore ? ["src/Core/Core.csproj"] : [])
                .Concat(directories.Select(directory => directory + "/.gitkeep"));
            var conflicts = generated.Select(path => Path.Combine(root, path))
                .Where(path => File.Exists(path) || Directory.Exists(path)).ToList();
            conflicts.AddRange(selectedResources.Select(resource => resource.Path)
                .Where(path => preserve.Contains(path, StringComparer.OrdinalIgnoreCase))
                .Append(createProps ? "Directory.Build.props" : "")
                .Where(path => path.Length > 0)
                .Select(path => ExistingRootPath(root, path))
                .Where(path => Directory.Exists(path)));
            if (createSolution)
                conflicts.AddRange(Directory.EnumerateFiles(root, "*.sln*", SearchOption.TopDirectoryOnly));
            if (conflicts.Count > 0)
                throw new InvalidOperationException("Boilerplate would overwrite existing files: " + string.Join(", ", conflicts));

            var parametersPath = Path.Combine(root, ".fallout", "parameters.json");
            JsonObject? parameters = null;
            if (createSolution)
            {
                if (!File.Exists(Path.Combine(root, "build", "_build.csproj")))
                    throw new InvalidOperationException("Create build/_build.csproj before running SetupBoilerplate; see the package README.");
                parameters = File.Exists(parametersPath)
                    ? JsonNode.Parse(File.ReadAllText(parametersPath)) as JsonObject
                    ?? throw new InvalidOperationException("Fallout parameters.json must contain a JSON object.")
                    : new JsonObject();
                if (parameters.Any(property => string.Equals(property.Key, "Solution", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Fallout parameters.json already defines a solution; it will not be overwritten.");
            }

            foreach (var resource in selectedResources)
            {
                var destination = preserve.Contains(resource.Path, StringComparer.OrdinalIgnoreCase)
                    ? ExistingRootPath(root, resource.Path) : Path.Combine(root, resource.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                using var input = assembly.GetManifestResourceStream(resource.Name)!;
                if (File.Exists(destination))
                {
                    if (options.AppendExisting && resource.Path is ".gitignore" or ".editorconfig")
                    {
                        AppendOnce(destination, input);
                        Log.Information("Applied packaged settings to {File}", destination);
                    }
                    else
                    {
                        Log.Information("Preserving existing {File}", destination);
                    }
                    continue;
                }
                using var output = new FileStream(destination, FileMode.CreateNew);
                input.CopyTo(output);
                if (!OperatingSystem.IsWindows() && destination.EndsWith(".sh", StringComparison.Ordinal))
                    File.SetUnixFileMode(destination, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            if (createCore)
            {
                Directory.CreateDirectory(Path.Combine(root, "src", "Core"));
                WriteNewText(Path.Combine(root, "src", "Core", "Core.csproj"), """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <Nullable>enable</Nullable>
                        <ImplicitUsings>enable</ImplicitUsings>
                      </PropertyGroup>
                    </Project>

                    """);
            }
            foreach (var directory in directories)
            {
                Directory.CreateDirectory(Path.Combine(root, directory));
                WriteNewText(Path.Combine(root, directory, ".gitkeep"), "");
            }

            var propsPath = ExistingRootPath(root, "Directory.Build.props");
            if (createProps && !File.Exists(propsPath))
                WriteNewText(propsPath, "<Project>\n  <ItemGroup>\n    <EmbeddedResource Include=\"**\\Queries\\**\\*.sql\" />\n  </ItemGroup>\n</Project>\n");
            else if (createProps)
                Log.Information("Preserving existing {File}", propsPath);

            if (createSolution)
            {
                RunDotNet(root, "new", "sln", "--format", "sln", "--name", solutionName!);
                var projects = new List<string> { "sln", solutionName + ".sln", "add", "build/_build.csproj" };
                if (createCore) projects.Add("src/Core/Core.csproj");
                RunDotNet(root, projects.ToArray());
                Directory.CreateDirectory(Path.GetDirectoryName(parametersPath)!);
                parameters!["Solution"] = solutionName + ".sln";
                File.WriteAllText(parametersPath, parameters.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
            }
        }

        private static string[] ParseSelectors(string? value, bool defaultAll) => string.IsNullOrWhiteSpace(value)
            ? defaultAll ? ["all"] : []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => item.Replace('\\', '/').TrimEnd('/').ToLowerInvariant()).ToArray();

        private static bool Matches(string item, string selector) => selector == "all" ||
            string.Equals(item, selector, StringComparison.OrdinalIgnoreCase) ||
            (selector.StartsWith("scripts", StringComparison.OrdinalIgnoreCase) &&
             item.StartsWith(selector + "/", StringComparison.OrdinalIgnoreCase));

        private static string ExistingRootPath(string root, string name) =>
            Directory.EnumerateFileSystemEntries(root)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), name, StringComparison.OrdinalIgnoreCase))
            ?? Path.Combine(root, name);

        private static void AppendOnce(string destination, Stream input)
        {
            using var memory = new MemoryStream();
            input.CopyTo(memory);
            var addition = memory.ToArray();
            var existing = File.ReadAllBytes(destination);
            if (existing.AsSpan().IndexOf(addition) >= 0) return;
            using var output = new FileStream(destination, FileMode.Append, FileAccess.Write);
            if (existing.Length > 0 && existing[^1] != (byte)'\n') output.WriteByte((byte)'\n');
            if (existing.Length > 0) output.WriteByte((byte)'\n');
            output.Write(addition);
        }

        private static void WriteNewText(string path, string content)
        {
            using var output = new StreamWriter(new FileStream(path, FileMode.CreateNew));
            output.Write(content);
        }

        private static void RunDotNet(string root, params string[] arguments)
        {
            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet.");
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException($"dotnet {string.Join(" ", arguments)} failed with exit code {process.ExitCode}.");
        }

        [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
        private static partial Regex SolutionNameRegex();
    }
}
