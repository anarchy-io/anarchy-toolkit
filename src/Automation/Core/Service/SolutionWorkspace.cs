using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Helpers.Builds;
using Anarchy.Toolkit.Core.Helpers.Ports;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// An existing Anarchy solution: a directory whose Fallout <c>.fallout/parameters.json</c> names its solution file,
/// with the layout from its optional root preferences file, the names of the projects under the layout's root and the
/// local ports its web hosts already listen on.
/// </summary>
/// <remarks>Used by <see cref="ApplicationScaffolder.Add"/> to add applications to a solution that already exists.</remarks>
/// <param name="Root">The full path of the solution directory.</param>
/// <param name="SolutionFile">The full path of the solution file.</param>
/// <param name="Layout">The effective layout resolved from optional preferences.</param>
/// <param name="ProjectNames">The names of the projects under the layout's root.</param>
/// <param name="UsedPorts">The local ports already used by <c>RunWebHost</c> calls in the solution's legacy and explicit entry-point files.</param>
public sealed partial record SolutionWorkspace(
    string Root,
    string SolutionFile,
    SolutionLayout Layout,
    IReadOnlyCollection<string> ProjectNames,
    IReadOnlyCollection<int> UsedPorts)
{
    /// <summary>
    /// The directory of the solution containing <paramref name="directory"/>, searching upwards, or null if there is none.
    /// </summary>
    /// <param name="directory">The directory to start from.</param>
    /// <returns>
    /// The nearest directory whose <c>.fallout/parameters.json</c> defines a <c>Solution</c>, or <see langword="null"/>.
    /// </returns>
    public static string? FindRoot(string directory)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(directory)); current is not null; current = current.Parent)
        {
            if (File.Exists(BuildEngineParameters.PathOf(current.FullName)) &&
                BuildEngineParameters.GetString(BuildEngineParameters.Read(current.FullName), "Solution") is not null)
                return current.FullName;
        }
        return null;
    }

    /// <summary>
    /// The workspace containing <paramref name="directory"/>, searching upwards (see <see cref="FindRoot"/>), or null
    /// if there is none.
    /// </summary>
    /// <param name="directory">The directory to start from.</param>
    /// <returns>The nearest workspace, or <see langword="null"/>.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the solution's preferences or effective layout are invalid (see <see cref="Load"/>).
    /// </exception>
    public static SolutionWorkspace? Find(string directory) => FindRoot(directory) is { } root ? Load(root) : null;

    /// <summary>
    /// Reads the workspace at <paramref name="root"/>: the solution from Fallout's parameters and its effective
    /// optional preferences layout, plus projects and used ports under the layout's root.
    /// </summary>
    /// <param name="root">The solution directory.</param>
    /// <param name="layoutOverrides">Explicit layout fields that take precedence over saved preferences.</param>
    /// <returns>The workspace.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when <c>.fallout/parameters.json</c> defines no solution, or preferences/effective layout are invalid.
    /// </exception>
    public static SolutionWorkspace Load(string root, JsonObject? layoutOverrides = null)
    {
        root = Path.GetFullPath(root);
        var parameters = BuildEngineParameters.Read(root);
        var solution = BuildEngineParameters.GetString(parameters, "Solution")
                       ?? throw new InvalidOperationException($"{BuildEngineParameters.PathOf(root)} defines no solution.");
        var layout = AutomationPreferences.Read(root).ResolveLayout(layoutOverrides);

        var source = Path.Combine(root, layout.Root);
        var files = Directory.Exists(source)
            ? Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories)
                .Where(file => !Path.GetRelativePath(source, file).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
                .ToArray()
            : [];
        var projects = files.Where(file => file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension).OfType<string>().ToArray();
        var ports = files.Where(file => Path.GetFileName(file) is "Program.cs" or "EntryPointLocal.cs")
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
