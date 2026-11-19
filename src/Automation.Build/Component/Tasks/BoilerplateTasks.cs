using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Serilog;

namespace Anarchy.Toolkit.Automation.Build.Component.Tasks;

/// <summary>
/// Creates a minimal solution and installs the packaged build scripts without replacing existing files.
/// See <see cref="SolutionScaffolder"/> in <c>Anarchy.Toolkit.Automation</c>.
/// </summary>
/// <remarks>
/// Progress is logged through Serilog. Used by <see cref="IBoilerplateBuild.SetupBoilerplate"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// BoilerplateTasks.Create(RootDirectory, "Shop", new BoilerplateOptions { Exclude = "samples,docs" });
/// </code>
/// </example>
public static class BoilerplateTasks
{
    /// <summary>
    /// Creates the starter solution in <paramref name="root"/> with every scaffold item.
    /// </summary>
    /// <param name="root">The directory to create the solution in.</param>
    /// <param name="solutionName">The name of the solution, or <see langword="null"/> to let the scaffolder choose its default.</param>
    public static void Create(string root, string? solutionName) => Create(root, solutionName, new BoilerplateOptions());

    /// <summary>
    /// Creates the starter solution in <paramref name="root"/> with the scaffold items selected by
    /// <paramref name="options"/>.
    /// </summary>
    /// <param name="root">The directory to create the solution in.</param>
    /// <param name="solutionName">The name of the solution, or <see langword="null"/> to let the scaffolder choose its default.</param>
    /// <param name="options">The items to include or exclude, and whether to append to existing files.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    public static void Create(string root, string? solutionName, BoilerplateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        SolutionScaffolder.Create(root, solutionName, new ScaffoldOptions {
            Items = options.Items,
            Exclude = options.Exclude,
            AppendExisting = options.AppendExisting,
            DryRun = options.DryRun,
            Log = message => Log.Information("{Message}", message)
        });
    }

    /// <summary>
    /// Logs how the solution's copies of the packaged files differ from the packaged ones, as unified diffs; see
    /// <see cref="BoilerplateCatalog.Compare"/>.
    /// </summary>
    /// <param name="root">The solution root.</param>
    /// <param name="paths">Comma-separated packaged paths or folders; <see langword="null"/> for every file but the agent skills.</param>
    /// <returns>The number of files that are missing or differ.</returns>
    public static int Diff(string root, string? paths)
    {
        var selected = paths?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var comparisons = BoilerplateCatalog.Compare(root, selected);
        foreach (var file in comparisons)
        {
            if (file.Status == BoilerplateCatalog.FileStatus.Different)
                Log.Information("{Path}: differs from the packaged file\n{Diff}", file.Path, file.Diff);
            else
                Log.Information("{Path}: {Status}", file.Path, file.Status == BoilerplateCatalog.FileStatus.Missing ? "missing" : "identical");
        }
        return comparisons.Count(file => file.Status != BoilerplateCatalog.FileStatus.Identical);
    }
}
