using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using System.Text.RegularExpressions;
using LocalPorts = Anarchy.Toolkit.Core.Helper.LocalPorts;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// An existing Anarchy solution: a directory whose <c>.fallout/parameters.json</c> names its solution file, with the
/// names of the projects under <c>src</c> and the local ports its web hosts already listen on.
/// </summary>
/// <remarks>Used by <see cref="ApplicationScaffolder.Add"/> to add applications to a solution that already exists.</remarks>
/// <param name="Root">The full path of the solution directory.</param>
/// <param name="SolutionFile">The full path of the solution file.</param>
/// <param name="Layout">The layout, read from the <c>ILayeringBuild</c> parameters.</param>
/// <param name="ProjectNames">The names of the projects under <c>src</c>.</param>
/// <param name="UsedPorts">The local ports already used by <c>RunWebHost</c> calls in the solution's <c>Program.cs</c> files.</param>
public sealed partial record SolutionWorkspace(
    string Root,
    string SolutionFile,
    SolutionLayout Layout,
    IReadOnlyCollection<string> ProjectNames,
    IReadOnlyCollection<int> UsedPorts)
{
    /// <summary>
    /// The workspace containing <paramref name="directory"/>, searching upwards, or null if there is none.
    /// </summary>
    /// <param name="directory">The directory to start from.</param>
    /// <returns>
    /// The nearest workspace whose <c>.fallout/parameters.json</c> defines a <c>Solution</c>, or <see langword="null"/>.
    /// </returns>
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

    /// <summary>
    /// Reads the workspace at <paramref name="root"/>: the layout from the <c>ILayeringBuild</c> parameters, and the
    /// projects and used ports from the files under <c>src</c> (<c>bin</c>/<c>obj</c> excluded).
    /// </summary>
    /// <remarks>
    /// Without <c>LayeringByFolder</c> the layout is <see cref="LayeringMode.Prefix"/>, and without
    /// <c>LayeringLayers</c> its layers are <see cref="SolutionLayout.DefaultLayers"/>, as in <c>ILayeringBuild</c>.
    /// </remarks>
    /// <param name="root">The solution directory.</param>
    /// <returns>The workspace.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when <c>.fallout/parameters.json</c> defines no solution.</exception>
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

    /// <summary>
    /// <c>RunWebHost&lt;Startup&gt;(args)</c> (default ports) or <c>RunWebHost&lt;Startup&gt;(args, 5002, 5003)</c>.
    /// </summary>
    [GeneratedRegex(@"RunWebHost<[^>]+>\(\s*args\s*(?:,\s*(\d+)\s*,\s*(\d+)\s*)?\)")]
    private static partial Regex RunWebHostRegex();
}
