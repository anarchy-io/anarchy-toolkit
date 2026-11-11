using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    /// <summary>Creates a starter solution without requiring an existing solution.</summary>
    public interface IBoilerplateBuild : IFalloutBuild
    {
        [Parameter("Name of the solution to create")]
        string? BoilerplateSolutionName => TryGetValue(() => BoilerplateSolutionName);

        [Parameter("Comma-separated boilerplate items to include (default: all)")]
        string? BoilerplateItems => TryGetValue(() => BoilerplateItems);

        [Parameter("Comma-separated boilerplate items to exclude")]
        string? BoilerplateExclude => TryGetValue(() => BoilerplateExclude);

        [Parameter("Append packaged .gitignore and .editorconfig to existing files")]
        bool AppendExisting => TryGetValue<bool?>(() => AppendExisting) ?? false;

        [Parameter("Alias for --append-existing", Name = "apend-existing")]
        bool ApendExisting => TryGetValue<bool?>(() => ApendExisting) ?? false;

        Target SetupBoilerplate => d => d
            .Description("Creates an Anarchy-style starter solution, scripts and Makefile")
            .Executes(() => BoilerplateTasks.Create(RootDirectory, BoilerplateSolutionName, new BoilerplateOptions {
                Items = BoilerplateItems,
                Exclude = BoilerplateExclude,
                AppendExisting = AppendExisting || ApendExisting
            }));
    }
}
