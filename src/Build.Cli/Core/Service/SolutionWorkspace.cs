using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// An existing Anarchy solution: a directory whose <c>.fallout/parameters.json</c> names its solution file, with the
/// names of the projects under <c>src</c> and the local ports its web hosts already listen on.
/// </summary>
public sealed partial record SolutionWorkspace(
    string Root,
    string SolutionFile,
    SolutionLayout Layout,
    IReadOnlyCollection<string> ProjectNames,
    IReadOnlyCollection<int> UsedPorts)
{
    /// <summary>The workspace containing <paramref name="directory"/>, searching upwards, or null if there is none.</summary>
    public static SolutionWorkspace? Find(string directory)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(directory)); current is not null; current = current.Parent)
        {
            if (File.Exists(FalloutParameters.PathOf(current.FullName)) &&
                FalloutParameters.GetString(FalloutParameters.Read(current.FullName), "Solution") is not null)
                return Load(current.FullName);
        }
        return null;
    }

    public static SolutionWorkspace Load(string root)
    {
        root = Path.GetFullPath(root);
        var parameters = FalloutParameters.Read(root);
        var solution = FalloutParameters.GetString(parameters, "Solution")
                       ?? throw new InvalidOperationException($"{FalloutParameters.PathOf(root)} defines no solution.");
        // The same defaults as ILayeringBuild: name prefixes unless LayeringByFolder is set.
        var layout = new SolutionLayout(
            FalloutParameters.GetBool(parameters, "LayeringByFolder") == true ? LayeringMode.Folder : LayeringMode.Prefix,
            FalloutParameters.GetStrings(parameters, "LayeringLayers") ?? SolutionLayout.DefaultLayers);

        var source = Path.Combine(root, "src");
        var files = Directory.Exists(source)
            ? Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories)
                .Where(file => !Path.GetRelativePath(source, file).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
                .ToArray()
            : [];
        var projects = files.Where(file => file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension).OfType<string>().ToArray();
        var ports = files.Where(file => Path.GetFileName(file) == "Program.cs")
            .SelectMany(file => RunWebHostRegex().Matches(File.ReadAllText(file)))
            .SelectMany(match => match.Groups[1].Success
                ? new[] { int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value) }
                : [LocalPorts.First, LocalPorts.First + 1])
            .ToHashSet();

        return new SolutionWorkspace(root, Path.Combine(root, solution), layout, projects, ports);
    }

    /// <summary><c>RunWebHost&lt;Startup&gt;(args)</c> (default ports) or <c>RunWebHost&lt;Startup&gt;(args, 5002, 5003)</c>.</summary>
    [GeneratedRegex(@"RunWebHost<[^>]+>\(\s*args\s*(?:,\s*(\d+)\s*,\s*(\d+)\s*)?\)")]
    private static partial Regex RunWebHostRegex();
}
