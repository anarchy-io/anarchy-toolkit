using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Serilog;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks;

/// <summary>
/// Creates a minimal solution and installs the packaged build scripts without replacing existing files.
/// See <see cref="SolutionScaffolder"/> in <c>Anarchy.Toolkit.Build.Cli</c>.
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
            Log = message => Log.Information("{Message}", message)
        });
    }
}
