using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// Creates a starter solution without requiring an existing solution.
/// </summary>
/// <remarks>
/// Unlike the other components, it derives from <see cref="IFalloutBuild"/> directly, so it works
/// before any solution exists.
/// </remarks>
public interface IBoilerplateBuild : IFalloutBuild
{
    /// <summary>
    /// The name of the solution to create (<c>--boilerplate-solution-name</c>); <see langword="null"/> lets the
    /// scaffolder choose its default.
    /// </summary>
    [Parameter("Name of the solution to create")]
    string? BoilerplateSolutionName => TryGetValue(() => BoilerplateSolutionName);

    /// <summary>
    /// Comma-separated boilerplate items or script paths to include (<c>--boilerplate-items</c>); all when not set.
    /// See <see cref="Tasks.BoilerplateOptions.Items"/>.
    /// </summary>
    [Parameter("Comma-separated boilerplate items to include (default: all)")]
    string? BoilerplateItems => TryGetValue(() => BoilerplateItems);

    /// <summary>
    /// Comma-separated boilerplate items or script paths to exclude (<c>--boilerplate-exclude</c>).
    /// See <see cref="Tasks.BoilerplateOptions.Exclude"/>.
    /// </summary>
    [Parameter("Comma-separated boilerplate items to exclude")]
    string? BoilerplateExclude => TryGetValue(() => BoilerplateExclude);

    /// <summary>
    /// Whether to append to existing <c>.gitignore</c>, <c>.editorconfig</c> and <c>global.json</c> files
    /// (<c>--append-existing</c>) instead of leaving them untouched. Defaults to <see langword="false"/>.
    /// See <see cref="Tasks.BoilerplateOptions.AppendExisting"/>.
    /// </summary>
    [Parameter("Append packaged .gitignore and .editorconfig to existing files, and add the packaged test runner to global.json")]
    bool AppendExisting => TryGetValue<bool?>(() => AppendExisting) ?? false;

    /// <summary>
    /// Misspelled alias of <see cref="AppendExisting"/>, accepted as <c>--apend-existing</c>.
    /// </summary>
    [Parameter("Alias for --append-existing", Name = "apend-existing")]
    bool ApendExisting => TryGetValue<bool?>(() => ApendExisting) ?? false;

    /// <summary>
    /// Creates the starter solution, scripts and Makefile in <see cref="FalloutBuild.RootDirectory"/>
    /// through <see cref="Tasks.BoilerplateTasks.Create(string, string?, Tasks.BoilerplateOptions)"/>. Existing files are
    /// not replaced.
    /// </summary>
    Target SetupBoilerplate => d => d
        .Description("Creates an Anarchy-style starter solution, scripts and Makefile")
        .Executes(() => BoilerplateTasks.Create(RootDirectory, BoilerplateSolutionName, new BoilerplateOptions {
            Items = BoilerplateItems,
            Exclude = BoilerplateExclude,
            AppendExisting = AppendExisting || ApendExisting
        }));
}
