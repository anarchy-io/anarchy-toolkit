using Anarchy.Toolkit.Automation.Core.Entity;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Build.Cli.Command;

/// <summary>
/// The parsed command line. Without a solution name (bootstrap) or application (scaffold), the matching wizard runs.
/// </summary>
/// <param name="Mode">The command to run.</param>
/// <param name="SolutionName">The solution name given to <c>bootstrap</c>.</param>
/// <param name="OutputDirectory">The <c>--output</c> directory for bootstrap, skills install or config editing.</param>
/// <param name="Application">The application given to <c>scaffold</c>.</param>
/// <param name="Error">Why the command line is invalid, when <paramref name="Mode"/> is <see cref="ToolMode.Invalid"/>.</param>
public sealed record ToolCommand(
    ToolMode Mode,
    string? SolutionName = null,
    string? OutputDirectory = null,
    ApplicationSpec? Application = null,
    string? Error = null)
{
    /// <summary>
    /// Whether <c>bootstrap</c> installs the toolkit's agent skills (<c>--with-skills</c>).
    /// </summary>
    public bool AgentSkills { get; init; }

    /// <summary>
    /// Whether <c>bootstrap</c> creates the Unit, Integration and EndToEnd test projects; <c>--no-tests</c> turns it off.
    /// </summary>
    public bool TestProjects { get; init; } = true;

    /// <summary>
    /// Whether <c>bootstrap</c> creates a mock server host (<c>--with-mock-server</c>).
    /// </summary>
    public bool MockServer { get; init; }

    /// <summary>
    /// Whether <c>bootstrap</c> gives each application its own settings (<c>--isolated-app-settings</c>) instead of
    /// sharing one <c>AppSettings</c>, which it does whenever the layout supports it.
    /// </summary>
    public bool IsolatedAppSettings { get; init; }

    /// <summary>
    /// The layering mode requested for non-interactive bootstrap, or <see langword="null"/> for the default.
    /// </summary>
    public LayeringMode? Layering { get; init; }

    /// <summary>The named starting layout explicitly requested for bootstrap or scaffold.</summary>
    public SolutionLayoutPreset? LayoutPreset { get; init; }

    /// <summary>The folder segment used by the single-app preset, defaulting to <c>App</c>.</summary>
    public string? ApplicationFolder { get; init; }

    /// <summary>The explicitly supplied layering fields, preserving omission separately from saved/default values.</summary>
    public JsonObject? LayeringOverrides { get; init; }

    /// <summary>The scaffold target: application (the default), a library project, or a test project.</summary>
    public ScaffoldTarget ScaffoldTarget { get; init; }

    /// <summary>The library project name supplied to <c>scaffold library</c>.</summary>
    public string? LibraryName { get; init; }

    /// <summary>The layer supplied to <c>scaffold library --layer</c>.</summary>
    public string? LibraryLayer { get; init; }

    /// <summary>The test kind supplied to <c>scaffold tests</c>; null requests an interactive selection.</summary>
    public string? TestKind { get; init; }

    /// <summary>Whether scaffolding only previews the project without writing files or preferences.</summary>
    public bool DryRun { get; init; }

    /// <summary>Explicit Swagger choice; null means use saved preferences or the toolkit default.</summary>
    public bool? SwaggerOverride { get; init; }

    /// <summary>Explicit Swagger theme choice; null means use saved preferences or the toolkit default.</summary>
    public bool? SwaggerThemeOverride { get; init; }

    /// <summary>Whether to persist the deliberately selected preferences after successful generation.</summary>
    public bool SavePreferences { get; init; }

    /// <summary>The config subcommand: show, set, unset or configure.</summary>
    public string? ConfigAction { get; init; }

    /// <summary>The preference key for config set/unset.</summary>
    public string? PreferenceKey { get; init; }

    /// <summary>The JSON value for config set.</summary>
    public string? PreferenceValue { get; init; }

    /// <summary>
    /// The <c>boilerplate</c> subcommand: <c>list</c>, <c>show</c> or <c>diff</c>.
    /// </summary>
    public string? BoilerplateAction { get; init; }

    /// <summary>
    /// The packaged paths given to <c>boilerplate show</c> (exactly one) or <c>boilerplate diff</c> (any).
    /// </summary>
    public IReadOnlyList<string> Paths { get; init; } = [];

    /// <summary>
    /// Whether the command needs a wizard, and therefore interactive input.
    /// </summary>
    public bool IsInteractive => (Mode == ToolMode.Bootstrap && SolutionName is null) ||
                                 (Mode == ToolMode.Scaffold && ScaffoldTarget == ScaffoldTarget.Application && Application is null) ||
                                 (Mode == ToolMode.Scaffold && ScaffoldTarget == ScaffoldTarget.Tests && TestKind is null) ||
                                 (Mode == ToolMode.Config && ConfigAction == "configure");
}
