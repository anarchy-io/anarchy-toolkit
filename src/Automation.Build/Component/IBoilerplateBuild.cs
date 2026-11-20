using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Fallout.Common;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Creates a starter solution without requiring an existing solution.
/// </summary>
/// <remarks>
/// It can be composed on <see cref="AnarchyBuild"/> before any solution exists.
/// </remarks>
public interface IBoilerplateBuild : IAutomationPreferencesBuild
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
    [Parameter("Comma-separated boilerplate items to include (default: all but the agent skills, which 'skills' selects)")]
    string? BoilerplateItems => TryGetValue(() => BoilerplateItems) ?? Preference<string>("boilerplate.items");

    /// <summary>
    /// Comma-separated boilerplate items or script paths to exclude (<c>--boilerplate-exclude</c>).
    /// See <see cref="Tasks.BoilerplateOptions.Exclude"/>.
    /// </summary>
    [Parameter("Comma-separated boilerplate items to exclude")]
    string? BoilerplateExclude => TryGetValue(() => BoilerplateExclude) ?? Preference<string>("boilerplate.exclude");

    /// <summary>
    /// Whether to sync the packaged settings into the managed block of existing <c>.gitignore</c> and
    /// <c>.editorconfig</c> files, and add the packaged test runner to <c>global.json</c> (<c>--append-existing</c>),
    /// instead of leaving them untouched. Defaults to <see langword="false"/>.
    /// See <see cref="Tasks.BoilerplateOptions.AppendExisting"/>.
    /// </summary>
    [Parameter("Sync the packaged .gitignore and .editorconfig into a managed block of existing files, and add the packaged test runner to global.json")]
    bool AppendExisting => TryGetValue<bool?>(() => AppendExisting) ?? false;

    /// <summary>
    /// Misspelled alias of <see cref="AppendExisting"/>, accepted as <c>--apend-existing</c>.
    /// </summary>
    [Parameter("Alias for --append-existing", Name = "apend-existing")]
    bool ApendExisting => TryGetValue<bool?>(() => ApendExisting) ?? false;

    /// <summary>
    /// Whether <see cref="SetupBoilerplate"/> only reports what it would do (<c>--boilerplate-dry-run</c>).
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [Parameter("Only report what SetupBoilerplate would create, keep, update or refuse, without writing")]
    bool BoilerplateDryRun => TryGetValue<bool?>(() => BoilerplateDryRun) ?? false;

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
            AppendExisting = AppendExisting || ApendExisting,
            SkillDirectories = AgentSkillsDirectories,
            DryRun = BoilerplateDryRun
        }));

    /// <summary>
    /// Shows how the solution's copies of the packaged boilerplate files (those selected by
    /// <see cref="BoilerplateItems"/>, or all but the agent skills) differ from the packaged ones, without writing.
    /// See <see cref="Tasks.BoilerplateTasks.Diff"/>.
    /// </summary>
    Target DiffBoilerplate => d => d
        .Description("Shows how the solution's boilerplate files differ from the packaged ones")
        .Executes(() => { BoilerplateTasks.Diff(RootDirectory, BoilerplateItems); });
}
