using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Serilog;

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

    /// <summary>
    /// Creates a minimal solution and installs the packaged build scripts without replacing existing files.
    /// See <see cref="SolutionScaffolder"/> in <c>Anarchy.Toolkit.Build.Cli</c>.
    /// </summary>
    public static class BoilerplateTasks
    {
        public static void Create(string root, string? solutionName) => Create(root, solutionName, new BoilerplateOptions());

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
}
