using Anarchy.Toolkit.Automation.Core.Entity;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Build.Cli.Command;

/// <summary>Parses <c>anarchy-toolkit</c> commands without hiding whether an option was explicitly supplied.</summary>
public static class ToolCommandLine
{
    /// <summary>Usage for bootstrap, including optional layout overrides and preference saving.</summary>
    public const string BootstrapUsage = "anarchy-toolkit bootstrap [<solution-name> [--output <empty-directory>] [--with-skills] [--no-tests] [--with-mock-server] [--isolated-app-settings] [--layout monorepo|single-app] [--application-folder <name>] [layout options] [--save-preferences]]";
    /// <summary>Flag to install the toolkit's agent skills while bootstrapping.</summary>
    public const string WithSkills = "--with-skills";
    /// <summary>Flag to bootstrap without the Unit, Integration and EndToEnd test projects.</summary>
    public const string NoTests = "--no-tests";
    /// <summary>Flag to bootstrap a mock server host in <c>tests/MockServer</c>.</summary>
    public const string WithMockServer = "--with-mock-server";
    /// <summary>Flag to give each bootstrapped application its own settings instead of sharing one <c>AppSettings</c>.</summary>
    public const string IsolatedAppSettings = "--isolated-app-settings";
    /// <summary>Usage for installing the toolkit's skills.</summary>
    public const string SkillsUsage = "anarchy-toolkit skills install [--output <solution-directory>]";
    /// <summary>Usage for inspecting packaged boilerplate.</summary>
    public const string BoilerplateUsage = "anarchy-toolkit boilerplate list|show <path>|diff [<path>...]";
    /// <summary>Flag to scaffold an API without Swagger (<c>Anarchy.Toolkit.Runtime.Web.Swagger</c>).</summary>
    public const string NoSwagger = "--no-swagger";
    /// <summary>Flag to scaffold an API with Swagger, overriding a saved preference.</summary>
    public const string WithSwagger = "--swagger";
    /// <summary>Flag to disable the toolkit's Swagger UI theme for a scaffolded API.</summary>
    public const string NoSwaggerTheme = "--no-swagger-theme";
    /// <summary>Flag to enable the toolkit's Swagger UI theme for a scaffolded API.</summary>
    public const string SwaggerTheme = "--swagger-theme";
    /// <summary>Usage for application, library and test-project scaffolding.</summary>
    public const string ScaffoldUsage = "anarchy-toolkit scaffold api|job|ui <Name> [--container] [--lambda-api|--lambda-event] [--job-mode worker|once] [--swagger|--no-swagger] [--swagger-theme|--no-swagger-theme] | library <Name> --layer <Layer> | tests [unit|integration|endtoend] [--dry-run] [--save-preferences]";
    /// <summary>Usage for showing or editing preferences.</summary>
    public const string ConfigUsage = "anarchy-toolkit config show|set <section.property> <json-value>|unset <section.property>|configure [--output <solution-directory>]";

    private static readonly JsonDocumentOptions JsonOptions = new() {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        AllowDuplicateProperties = false
    };

    /// <summary>Parses arguments without the executable name; malformed input becomes an Invalid command.</summary>
    /// <param name="args">Arguments without the executable name.</param>
    /// <returns>The parsed command.</returns>
    public static ToolCommand Parse(IReadOnlyList<string> args) => args switch {
        [] or ["--help" or "-h" or "help", ..] => new ToolCommand(ToolMode.Help),
        ["bootstrap", ..] => ParseBootstrap(args.Skip(1).ToArray()),
        ["scaffold", ..] => ParseScaffold(args.Skip(1).ToArray()),
        ["skills", ..] => ParseSkills(args.Skip(1).ToArray()),
        ["config", ..] => ParseConfig(args.Skip(1).ToArray()),
        ["boilerplate", "--help" or "-h" or "help"] => new ToolCommand(ToolMode.Help),
        ["boilerplate", ..] => ParseBoilerplate(args.Skip(1).ToArray()),
        [var command, ..] => Invalid($"Unknown command '{command}'.")
    };

    private static ToolCommand ParseBootstrap(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return new ToolCommand(ToolMode.Bootstrap);
        if (args[0].StartsWith('-')) return Invalid("Usage: " + BootstrapUsage);

        string? output = null;
        var skills = false;
        var noTests = false;
        var mockServer = false;
        var isolated = false;
        var save = false;
        SolutionLayoutPreset? preset = null;
        string? applicationFolder = null;
        var overrides = new JsonObject();
        for (var index = 1; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--output" or "-o" when output is null && HasValue(args, index):
                    output = args[++index];
                    break;
                case WithSkills when !skills:
                    skills = true;
                    break;
                case NoTests when !noTests:
                    noTests = true;
                    break;
                case WithMockServer when !mockServer:
                    mockServer = true;
                    break;
                case IsolatedAppSettings when !isolated:
                    isolated = true;
                    break;
                case "--save-preferences" when !save:
                    save = true;
                    break;
                case "--layout" when preset is null && HasValue(args, index):
                    if (!TryParsePreset(args[++index], out var parsedPreset)) return Invalid("--layout must be 'monorepo' or 'single-app'.");
                    preset = parsedPreset;
                    break;
                case "--application-folder" when applicationFolder is null && HasValue(args, index):
                    applicationFolder = args[++index];
                    break;
                default:
                    if (!TryAddLayoutOption(args, ref index, overrides, BootstrapUsage, out var error)) return Invalid(error ?? "Usage: " + BootstrapUsage);
                    break;
            }
        }
        if (applicationFolder is not null && preset != SolutionLayoutPreset.SingleApp)
            return Invalid("--application-folder can only be used with --layout single-app.");
        var layering = ParseLayeringMode(overrides["mode"]?.GetValue<string>());
        return new ToolCommand(ToolMode.Bootstrap, args[0], output) {
            AgentSkills = skills,
            TestProjects = !noTests,
            MockServer = mockServer,
            IsolatedAppSettings = isolated,
            Layering = layering,
            LayoutPreset = preset,
            ApplicationFolder = applicationFolder,
            LayeringOverrides = overrides.Count == 0 ? null : overrides,
            SavePreferences = save
        };
    }

    private static ToolCommand ParseScaffold(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return new ToolCommand(ToolMode.Scaffold);

        ApplicationSpec? application = null;
        ScaffoldTarget target;
        string? libraryName = null;
        string? testKind = null;
        var index = 0;
        if (args[0] == "library")
        {
            target = ScaffoldTarget.Library;
            if (args.Count < 2 || args[1].StartsWith('-')) return Invalid("Usage: anarchy-toolkit scaffold library <Name> --layer <Layer> [layout options] [--dry-run] [--save-preferences]");
            libraryName = args[1];
            index = 2;
        }
        else if (args[0] == "tests")
        {
            target = ScaffoldTarget.Tests;
            index = 1;
            if (index < args.Count && !args[index].StartsWith('-'))
            {
                if (!TryParseTestKind(args[index], out testKind))
                    return Invalid("Test kind must be 'unit', 'integration' or 'endtoend'.");
                index++;
            }
        }
        else
        {
            target = ScaffoldTarget.Application;
            if (args.Count >= 2 && TryParseKind(args[0], out var kind) && !args[1].StartsWith('-'))
            {
                application = new ApplicationSpec(kind, args[1]);
                index = 2;
            }
            else if (!args[0].StartsWith('-')) return Invalid("Usage: " + ScaffoldUsage);
        }

        bool? swagger = null;
        bool? swaggerTheme = null;
        var container = false;
        var apiLambda = false;
        var eventLambda = false;
        JobExecutionMode? jobMode = null;
        var save = false;
        var dryRun = false;
        string? layer = null;
        SolutionLayoutPreset? preset = null;
        string? applicationFolder = null;
        var overrides = new JsonObject();
        for (; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--container" when !container && application?.IsWebHost == true:
                    container = true;
                    break;
                case "--lambda-api" when !apiLambda && application?.Kind == ApplicationKind.Api:
                    apiLambda = true;
                    break;
                case "--lambda-event" when !eventLambda && application?.Kind == ApplicationKind.Job:
                    eventLambda = true;
                    break;
                case "--job-mode" when jobMode is null && application?.Kind == ApplicationKind.Job && HasValue(args, index):
                    var requestedMode = args[++index];
                    if (!string.Equals(requestedMode, "worker", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(requestedMode, "once", StringComparison.OrdinalIgnoreCase))
                        return Invalid("--job-mode must be 'worker' or 'once'.");
                    jobMode = string.Equals(requestedMode, "worker", StringComparison.OrdinalIgnoreCase)
                        ? JobExecutionMode.Worker
                        : JobExecutionMode.Once;
                    break;
                case NoSwagger when swagger is null && application?.Kind == ApplicationKind.Api:
                    swagger = false;
                    break;
                case WithSwagger when swagger is null && application?.Kind == ApplicationKind.Api:
                    swagger = true;
                    break;
                case NoSwaggerTheme when swaggerTheme is null && application?.Kind == ApplicationKind.Api:
                    swaggerTheme = false;
                    break;
                case SwaggerTheme when swaggerTheme is null && application?.Kind == ApplicationKind.Api:
                    swaggerTheme = true;
                    break;
                case "--layer" when target == ScaffoldTarget.Library && layer is null && HasValue(args, index):
                    layer = args[++index];
                    break;
                case "--dry-run" when target != ScaffoldTarget.Application && !dryRun:
                    dryRun = true;
                    break;
                case "--save-preferences" when !save:
                    save = true;
                    break;
                case "--layout" when preset is null && HasValue(args, index):
                    if (!TryParsePreset(args[++index], out var parsedPreset)) return Invalid("--layout must be 'monorepo' or 'single-app'.");
                    preset = parsedPreset;
                    break;
                case "--application-folder" when applicationFolder is null && HasValue(args, index):
                    applicationFolder = args[++index];
                    break;
                default:
                    if (!TryAddLayoutOption(args, ref index, overrides, ScaffoldUsage, out var error)) return Invalid(error ?? "Usage: " + ScaffoldUsage);
                    break;
            }
        }
        if (swagger == false && swaggerTheme is not null)
            return Invalid($"{NoSwagger} cannot be combined with {SwaggerTheme} or {NoSwaggerTheme}.");
        if (target == ScaffoldTarget.Library && layer is null) return Invalid("scaffold library requires --layer <Layer>.");
        if (target != ScaffoldTarget.Library && layer is not null) return Invalid("--layer is only valid for scaffold library.");
        if (applicationFolder is not null && preset != SolutionLayoutPreset.SingleApp)
            return Invalid("--application-folder can only be used with --layout single-app.");
        if (application is not null)
            application = application with { Container = container, ApiLambda = apiLambda, EventLambda = eventLambda, JobMode = jobMode ?? JobExecutionMode.Worker };
        return new ToolCommand(ToolMode.Scaffold, Application: application) {
            ScaffoldTarget = target,
            LibraryName = libraryName,
            LibraryLayer = layer,
            TestKind = testKind,
            DryRun = dryRun,
            Layering = ParseLayeringMode(overrides["mode"]?.GetValue<string>()),
            LayoutPreset = preset,
            ApplicationFolder = applicationFolder,
            SwaggerOverride = swagger,
            SwaggerThemeOverride = swaggerTheme,
            SavePreferences = save,
            LayeringOverrides = overrides.Count == 0 ? null : overrides
        };
    }

    private static ToolCommand ParseConfig(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return Invalid("Usage: " + ConfigUsage);
        var action = args[0];
        if (action is not ("show" or "set" or "unset" or "configure")) return Invalid("Usage: " + ConfigUsage);

        string? key = null;
        string? value = null;
        string? output = null;
        var index = 1;
        if (action is "set" or "unset")
        {
            if (index >= args.Count) return Invalid("Usage: " + ConfigUsage);
            key = args[index++];
            if (action == "set")
            {
                if (index >= args.Count) return Invalid("Usage: " + ConfigUsage);
                value = args[index++];
            }
        }
        if (index < args.Count)
        {
            if ((args[index] is "--output" or "-o") && index + 1 < args.Count &&
                !string.IsNullOrWhiteSpace(args[index + 1]) && !args[index + 1].StartsWith('-'))
                output = args[index + 1];
            else return Invalid("Usage: " + ConfigUsage);
            index += 2;
        }
        if (index != args.Count) return Invalid("Usage: " + ConfigUsage);
        return new ToolCommand(ToolMode.Config, OutputDirectory: output) {
            ConfigAction = action,
            PreferenceKey = key,
            PreferenceValue = value
        };
    }

    private static bool TryAddLayoutOption(IReadOnlyList<string> args, ref int index, JsonObject overrides, string usage, out string? error)
    {
        error = null;
        var option = args[index];
        if (option is not ("--layering" or "--layering-layers" or "--layering-root" or "--layering-library-root" or "--layering-application-folders"))
        {
            error = "Usage: " + usage;
            return false;
        }
        if (!HasValue(args, index))
        {
            error = $"{option} requires a value.";
            return false;
        }
        var property = option switch {
            "--layering" => "mode",
            "--layering-layers" => "layers",
            "--layering-root" => "root",
            "--layering-library-root" => "libraryRoot",
            _ => "applicationFolders"
        };
        if (overrides.ContainsKey(property))
        {
            error = $"{option} may be supplied only once.";
            return false;
        }

        var raw = args[++index];
        try
        {
            if (property == "mode")
            {
                var mode = raw.ToLowerInvariant();
                if (mode is not ("folder" or "prefix"))
                {
                    error = "--layering must be 'folder' or 'prefix'.";
                    return false;
                }
                overrides[property] = mode;
            }
            else if (property is "layers" or "applicationFolders")
            {
                var parsed = JsonNode.Parse(raw, documentOptions: JsonOptions);
                if ((property == "layers" && parsed is not JsonArray) || (property == "applicationFolders" && parsed is not JsonObject))
                {
                    error = $"{option} must be a JSON {(property == "layers" ? "array" : "object")}.";
                    return false;
                }
                overrides[property] = parsed!.DeepClone();
            }
            else overrides[property] = raw;
        }
        catch (JsonException exception)
        {
            error = $"Invalid JSON for {option}: {exception.Message}";
            return false;
        }
        return true;
    }

    private static ToolCommand ParseSkills(IReadOnlyList<string> args) => args switch {
        ["install"] => new ToolCommand(ToolMode.Skills),
        ["install", "--output" or "-o", var output] when !string.IsNullOrWhiteSpace(output) && !output.StartsWith('-') =>
            new ToolCommand(ToolMode.Skills, OutputDirectory: output),
        _ => Invalid("Usage: " + SkillsUsage)
    };

    private static ToolCommand ParseBoilerplate(IReadOnlyList<string> args) => args switch {
        ["list"] => new ToolCommand(ToolMode.Boilerplate) { BoilerplateAction = "list" },
        ["show", var path] when !path.StartsWith('-') => new ToolCommand(ToolMode.Boilerplate) { BoilerplateAction = "show", Paths = [path] },
        ["diff", ..] when args.Skip(1).All(path => !path.StartsWith('-')) =>
            new ToolCommand(ToolMode.Boilerplate) { BoilerplateAction = "diff", Paths = args.Skip(1).ToArray() },
        _ => Invalid("Usage: " + BoilerplateUsage)
    };

    private static bool HasValue(IReadOnlyList<string> args, int index) =>
        index + 1 < args.Count && !string.IsNullOrWhiteSpace(args[index + 1]) && !args[index + 1].StartsWith('-');

    private static bool TryParseKind(string value, out ApplicationKind kind) =>
        Enum.TryParse(value, ignoreCase: true, out kind) && Enum.IsDefined(kind);

    private static bool TryParseTestKind(string value, out string? kind)
    {
        kind = value.ToLowerInvariant() switch {
            "unit" => "Unit",
            "integration" => "Integration",
            "endtoend" => "EndToEnd",
            _ => null
        };
        return kind is not null;
    }

    private static LayeringMode? ParseLayeringMode(string? mode) => mode switch {
        "folder" => LayeringMode.Folder,
        "prefix" => LayeringMode.Prefix,
        _ => null
    };

    private static bool TryParsePreset(string value, out SolutionLayoutPreset preset)
    {
        preset = value.ToLowerInvariant() switch {
            "monorepo" => SolutionLayoutPreset.Monorepo,
            "single-app" => SolutionLayoutPreset.SingleApp,
            _ => (SolutionLayoutPreset)(-1)
        };
        return Enum.IsDefined(preset);
    }

    private static ToolCommand Invalid(string error) => new(ToolMode.Invalid, Error: error);
}
