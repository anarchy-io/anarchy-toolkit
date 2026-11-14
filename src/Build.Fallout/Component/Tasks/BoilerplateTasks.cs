using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Serilog;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks;

/// <summary>
/// Creates a minimal solution and installs the packaged build scripts without replacing existing files.
/// See <see cref="SolutionScaffolder"/> in <c>Anarchy.Toolkit.Build.Cli</c>.
/// </summary>
public static class BoilerplateTasks
{
    /// <summary>
    /// Creates the starter solution in <paramref name="root"/> with every scaffold item.
    /// </summary>
    public static void Create(string root, string? solutionName) => Create(root, solutionName, new BoilerplateOptions());

    /// <summary>
    /// Creates the starter solution in <paramref name="root"/> with the scaffold items selected by
    /// <paramref name="options"/>.
    /// </summary>
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
