using Anarchy.Toolkit.Automation.Build.Cli.Command;
using Anarchy.Toolkit.Automation.Build.Cli.Wizard;
using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Core.Validation;
using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Build.Cli.Service;

/// <summary>
/// Runs the tool: parses the command line (showing the help without a command), runs a wizard when needed, and
/// bootstraps, scaffolds, edits preferences or installs the agent skills. Bootstrapping defaults to the working
/// directory; scaffolding and installing the skills look for a solution from the working directory upwards.
/// </summary>
/// <remarks>
/// This is the entry point of the <c>anarchy-toolkit</c> .NET tool; <c>Program.cs</c> only calls
/// <see cref="CreateDefault"/> and <see cref="Run"/>. Expected failures (invalid names, I/O errors while generating)
/// are reported through the <see cref="IWizardPrompter"/> and turned into exit code <c>1</c> rather than thrown.
/// </remarks>
/// <param name="prompter">The console interactions used for output, prompts and wizards.</param>
/// <param name="versions">The toolkit package versions written into generated projects.</param>
/// <param name="workingDirectory">
/// The directory relative to which <c>bootstrap</c> resolves its output directory and from which <c>scaffold</c>
/// searches upwards for a solution.
/// </param>
/// <param name="output">
/// Where <c>boilerplate</c> writes plain, uncoloured output that scripts and agents can read; <see cref="Console.Out"/>
/// when <see langword="null"/>.
/// </param>
/// <example>
/// <code language="csharp">
/// return ToolApplication.CreateDefault().Run(args, interactive: !Console.IsInputRedirected);
/// </code>
/// </example>
public sealed class ToolApplication(IWizardPrompter prompter, ToolkitPackageVersions versions, string workingDirectory, TextWriter? output = null)
{
    private const string ReviewSettings = "Review each application's settings (auth service URLs, client IDs) before running it.";
    private static readonly JsonDocumentOptions PreferenceJsonOptions = new() {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        AllowDuplicateProperties = false
    };

    /// <summary>
    /// Creates the application for the real console, the package versions embedded in this assembly and the current
    /// directory.
    /// </summary>
    /// <returns>A new <see cref="ToolApplication"/> using <see cref="SpectreWizardPrompter"/> on <see cref="AnsiConsole.Console"/>.</returns>
    public static ToolApplication CreateDefault() => new(
        new SpectreWizardPrompter(AnsiConsole.Console),
        ToolkitPackageVersionReader.FromAssembly(typeof(ToolApplication).Assembly),
        Directory.GetCurrentDirectory());

    /// <summary>
    /// Runs the command given by <paramref name="args"/>.
    /// </summary>
    /// <param name="args">The command-line arguments, without the executable name.</param>
    /// <param name="interactive">
    /// Whether input can be prompted for; when <c>false</c>, commands that need a wizard fail instead.
    /// </param>
    /// <returns>
    /// The process exit code: <c>0</c> on success or help; <c>1</c> on invalid input, failure or cancellation.
    /// </returns>
    public int Run(IReadOnlyList<string> args, bool interactive)
    {
        var command = ToolCommandLine.Parse(args);
        if (command.Mode == ToolMode.Boilerplate) return Boilerplate(command);
        if (command.Mode != ToolMode.Invalid && !(command.Mode == ToolMode.Config && command.ConfigAction == "show"))
            prompter.ShowBanner();
        if (command.Mode == ToolMode.Help)
        {
            prompter.ShowHelp();
            return 0;
        }
        if (command.Mode == ToolMode.Invalid)
        {
            prompter.Error(command.Error!);
            return 1;
        }
        if (command.IsInteractive && !interactive)
        {
            prompter.Error($"Interactive input is unavailable. Usage: {ToolCommandLine.BootstrapUsage} or {ToolCommandLine.ScaffoldUsage}");
            return 1;
        }

        if (command.Mode == ToolMode.Config) return Configure(command);

        var root = SolutionWorkspace.FindRoot(workingDirectory);
        return command.Mode switch {
            ToolMode.Bootstrap => Bootstrap(command),
            ToolMode.Skills => InstallSkills(command.OutputDirectory is { } output
                ? Path.GetFullPath(output, workingDirectory)
                : root ?? workingDirectory),
            _ when root is null => Fail("No Anarchy solution (.fallout/parameters.json with a Solution) found here or above; run 'anarchy-toolkit bootstrap' first."),
            _ => Scaffold(root, command)
        };
    }

    private int Bootstrap(ToolCommand command)
    {
        SolutionPlan? plan;
        var interactiveWizard = command.SolutionName is null;
        if (command.SolutionName is null)
        {
            plan = new BootstrapWizard(prompter).Run();
            if (plan is null) return Cancelled();
        }
        else if (NameRules.IsValidSolutionName(command.SolutionName))
        {
            SolutionLayout layout;
            try
            {
                var overrides = command.LayeringOverrides is null
                    ? new JsonObject()
                    : (JsonObject)command.LayeringOverrides.DeepClone();
                if (command.Layering is { } mode) overrides["mode"] = mode == LayeringMode.Folder ? "folder" : "prefix";
                layout = AutomationPreferences.Parse("{}").ResolveLayout(overrides);
            }
            catch (Exception exception) when (exception is InvalidOperationException or JsonException or ArgumentException)
            {
                return Fail(exception.Message);
            }
            plan = new SolutionPlan(command.SolutionName, Path.GetFullPath(command.OutputDirectory ?? workingDirectory, workingDirectory), layout, []) {
                AgentSkills = command.AgentSkills,
                SavePreferences = command.SavePreferences
            };
        }
        else
        {
            return Fail(NameRules.SolutionNameHelp);
        }

        if (SolutionBootstrapper.Validate(plan) is { } invalid) return Fail(invalid);
        if (!TryRun($"Creating {plan.Name}", progress => SolutionBootstrapper.Create(plan, versions, progress))) return 1;

        try
        {
            if (interactiveWizard && plan.SavePreferences)
                SaveLayoutPreferences(plan.RootDirectory, new JsonObject {
                    ["mode"] = plan.Layout.Mode == LayeringMode.Folder ? "folder" : "prefix",
                    ["layers"] = JsonSerializer.SerializeToNode(plan.Layout.Layers)
                });
            else if (command.SavePreferences && command.LayeringOverrides is { Count: > 0 } selected)
                SaveLayoutPreferences(plan.RootDirectory, selected);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Fail($"Solution created, but preferences could not be saved: {exception.Message}");
        }

        var layoutPreferencesSaved = interactiveWizard
            ? plan.SavePreferences
            : command.SavePreferences && command.LayeringOverrides is { Count: > 0 };
        var steps = new List<string> { $"cd {plan.RootDirectory}", "make compile" };
        if (!layoutPreferencesSaved && !IsDefaultLayout(plan.Layout))
        {
            var (cliArguments, anarchyArguments) = LayeringSuggestions(plan.Layout);
            steps.Add(MakeAnarchyTarget(anarchyArguments));
            steps.Add($"anarchy-toolkit scaffold api NewApi {cliArguments}");
        }
        else steps.Add("make check_layering");
        steps.AddRange(plan.Applications.Where(app => app.IsWebHost).Select(app => $"dotnet run --project {plan.Layout.ProjectPath(app)}"));
        steps.Add("anarchy-toolkit scaffold    # add more APIs, jobs or UIs later");
        prompter.ShowNextSteps($"{plan.Name} is ready", steps, plan.Applications.Count > 0 ? ReviewSettings : null);
        return 0;
    }

    private int Boilerplate(ToolCommand command)
    {
        var writer = output ?? Console.Out;
        try
        {
            switch (command.BoilerplateAction)
            {
                case "list":
                    foreach (var path in BoilerplateCatalog.List()) writer.WriteLine(path);
                    return 0;
                case "show":
                    if (BoilerplateCatalog.Read(command.Paths[0]) is not { } text)
                        return Fail($"No packaged boilerplate file '{command.Paths[0]}'; 'anarchy-toolkit boilerplate list' lists them.");
                    writer.Write(text);
                    return 0;
                default:
                    var root = SolutionWorkspace.FindRoot(workingDirectory) ?? workingDirectory;
                    var comparisons = BoilerplateCatalog.Compare(root, command.Paths);
                    foreach (var file in comparisons)
                    {
                        if (file.Status == BoilerplateCatalog.FileStatus.Different) writer.Write(file.Diff);
                        else writer.WriteLine($"{(file.Status == BoilerplateCatalog.FileStatus.Missing ? "missing" : "identical")}: {file.Path}");
                    }
                    return comparisons.Any(file => file.Status != BoilerplateCatalog.FileStatus.Identical) ? 1 : 0;
            }
        }
        catch (ArgumentException exception)
        {
            return Fail(exception.Message);
        }
    }

    private int InstallSkills(string root)
    {
        if (!Directory.Exists(root)) return Fail($"{root} does not exist.");
        string[] directories;
        try
        {
            directories = AutomationPreferences.Read(root).Get<string[]>("agentSkills.directories")
                          ?? [AgentSkillTasks.DefaultDirectory];
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return Fail(exception.Message);
        }
        if (!TryRun("Installing the agent skills", progress =>
                SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = "skills", SkillDirectories = directories, Log = progress })))
            return 1;

        var destination = directories.FirstOrDefault() ?? "(no destination configured)";
        var customDirectories = !directories.SequenceEqual([AgentSkillTasks.DefaultDirectory], StringComparer.Ordinal);
        var skillArguments = customDirectories
            ? $" --AgentSkillsDirectories {string.Join(' ', directories.Select(ShellQuote))}"
            : string.Empty;
        var note = customDirectories
            ? "Skill directories are ordered; the first is the creation destination. The suggested Anarchy targets pass the full ordered list explicitly."
            : null;
        var validateSkills = MakeAnarchyTarget($"ValidateAgentSkills{skillArguments}");
        var syncSkills = MakeAnarchyTarget($"SyncAgentSkills{skillArguments}");
        prompter.ShowNextSteps($"Agent skills installed in {Path.Combine(root, destination)}", [
            $"Commit {destination} so every agent working on the solution can use the skills",
            $"{validateSkills}    # check that the skills list the current make targets",
            $"{syncSkills}        # refresh commands and create skills for #@-marked targets"
        ], note);
        return 0;
    }

    private int Scaffold(string root, ToolCommand command)
    {
        SolutionWorkspace workspace;
        AutomationPreferences preferences;
        try
        {
            preferences = AutomationPreferences.Read(root);
            workspace = SolutionWorkspace.Load(root, command.LayeringOverrides);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException
                                              or System.Text.Json.JsonException)
        {
            return Fail(exception.Message);
        }

        var defaultSwaggerTheme = preferences.Get<bool?>("scaffolding.swaggerTheme") ?? true;
        var wizard = new ScaffoldWizard(prompter);
        var app = command.Application is null
            ? wizard.Run(workspace, defaultSwaggerTheme)
            : command.Application with { SwaggerTheme = command.SwaggerThemeOverride ?? defaultSwaggerTheme };
        if (app is null) return Cancelled();
        if (ApplicationScaffolder.Validate(workspace.Layout, app, workspace.ProjectNames) is { } invalid) return Fail(invalid);
        if (!TryRun($"Adding {app.Name}", progress => ApplicationScaffolder.Add(workspace, app, versions, progress))) return 1;

        var layoutPreferencesSaved = false;
        if (command.SavePreferences || wizard.SaveSwaggerThemePreference)
        {
            var selected = new JsonObject();
            if (command.SavePreferences && command.LayeringOverrides is { Count: > 0 } layoutOverrides)
                selected["layering"] = layoutOverrides.DeepClone();
            if (command.SavePreferences && command.SwaggerThemeOverride is { } explicitTheme)
                SetNested(selected, "scaffolding", "swaggerTheme", JsonValue.Create(explicitTheme));
            if (wizard.SaveSwaggerThemePreference)
                SetNested(selected, "scaffolding", "swaggerTheme", JsonValue.Create(app.SwaggerTheme));
            if (selected.Count > 0)
            {
                try { SaveSelectedPreferences(root, selected); }
                catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException)
                {
                    return Fail($"Application created, but preferences could not be saved: {exception.Message}");
                }
                layoutPreferencesSaved = selected["layering"] is JsonObject;
            }
        }

        var path = workspace.Layout.ProjectPath(app);
        var steps = new List<string> { "make compile" };
        var layoutWasAlreadySaved = LayoutsEqual(preferences.ResolveLayout(), workspace.Layout);
        if (!IsDefaultLayout(workspace.Layout) && !layoutPreferencesSaved && !layoutWasAlreadySaved)
        {
            var (cliArguments, anarchyArguments) = LayeringSuggestions(workspace.Layout);
            steps.Add(MakeAnarchyTarget(anarchyArguments));
            steps.Add($"anarchy-toolkit scaffold api NewApi {cliArguments}");
        }
        else steps.Add("make check_layering");
        if (app.IsWebHost) steps.Add($"dotnet run --project {path}");
        prompter.ShowNextSteps($"{path} is ready", steps, ReviewSettings);
        return 0;
    }

    private int Configure(ToolCommand command)
    {
        var root = command.OutputDirectory is { } outputDirectory
            ? Path.GetFullPath(outputDirectory, workingDirectory)
            : SolutionWorkspace.FindRoot(workingDirectory);
        if (root is null) return Fail("No Anarchy solution found here or above. Pass --output <solution-directory> to choose the preferences root.");
        if (!Directory.Exists(root)) return Fail($"{root} does not exist.");

        try
        {
            var preferences = AutomationPreferences.Read(root);
            switch (command.ConfigAction)
            {
                case "show":
                    (output ?? Console.Out).WriteLine(preferences.Describe());
                    return 0;
                case "set":
                    preferences.With(command.PreferenceKey!, command.PreferenceValue!).Save(root);
                    prompter.Info($"Saved {command.PreferenceKey} in {Path.Combine(root, AutomationPreferences.FileName)}.");
                    return 0;
                case "unset":
                    preferences.With(command.PreferenceKey!, null).Save(root);
                    prompter.Info($"Removed {command.PreferenceKey} from saved preferences.");
                    return 0;
                case "configure":
                    return ConfigureInteractively(root, preferences);
                default:
                    return Fail("Usage: " + ToolCommandLine.ConfigUsage);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException or JsonException)
        {
            return Fail(exception.Message);
        }
    }

    private int ConfigureInteractively(string root, AutomationPreferences current)
    {
        var keys = AutomationPreferences.Keys;
        var selected = prompter.SelectMany("Which saved preferences should be changed?", keys, key => key);
        if (selected.Count == 0)
        {
            prompter.Info("No preferences selected; nothing was written.");
            return 0;
        }

        var edits = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var key in selected)
        {
            var value = prompter.Ask($"JSON value for {key} (or 'unset')", null, answer => {
                if (answer.Equals("unset", StringComparison.OrdinalIgnoreCase)) return null;
                try { JsonNode.Parse(answer, documentOptions: PreferenceJsonOptions); return null; }
                catch (JsonException exception) { return exception.Message; }
            });
            edits[key] = value.Equals("unset", StringComparison.OrdinalIgnoreCase) ? null : value;
        }

        var proposed = current.WithEdits(edits);
        prompter.Info("Proposed preferences (saved overrides and effective layout):");
        prompter.Info(proposed.Describe());
        if (!prompter.Confirm("Write these preferences?", false))
        {
            prompter.Info("Cancelled; no preferences were written.");
            return 0;
        }
        proposed.Save(root);
        prompter.Info($"Saved preferences to {Path.Combine(root, AutomationPreferences.FileName)}.");
        return 0;
    }

    private static void SaveLayoutPreferences(string root, JsonObject selected)
    {
        AutomationPreferences.Read(root).WithLayering(selected).Save(root);
    }

    private static void SaveSelectedPreferences(string root, JsonObject selected)
    {
        var preferences = AutomationPreferences.Read(root);
        if (selected["layering"] is JsonObject layering)
            preferences = preferences.WithLayering(layering);
        if (selected["scaffolding"] is JsonObject scaffolding)
        {
            foreach (var (key, value) in scaffolding)
                preferences = preferences.With("scaffolding." + key, value?.ToJsonString());
        }
        preferences.Save(root);
    }

    private static bool IsDefaultLayout(SolutionLayout layout)
    {
        var baseline = AutomationPreferences.Parse("{}").ResolveLayout();
        return LayoutsEqual(layout, baseline);
    }

    private static bool LayoutsEqual(SolutionLayout left, SolutionLayout right) =>
        left.Mode == right.Mode && left.Layers.SequenceEqual(right.Layers, StringComparer.Ordinal) &&
        string.Equals(left.Root, right.Root, StringComparison.Ordinal) &&
        string.Equals(left.LibraryRoot, right.LibraryRoot, StringComparison.Ordinal) &&
        left.ApplicationFolders.Count == right.ApplicationFolders.Count &&
        left.ApplicationFolders.OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .SequenceEqual(right.ApplicationFolders.OrderBy(entry => entry.Key, StringComparer.Ordinal));

    private static (string CliArguments, string AnarchyArguments) LayeringSuggestions(SolutionLayout layout)
    {
        var mode = layout.Mode == LayeringMode.Folder ? "folder" : "prefix";
        var layers = JsonSerializer.Serialize(layout.Layers);
        var cli = $"--layering {mode} --layering-layers {ShellQuote(layers)} --layering-root {ShellQuote(layout.Root)}";
        var anarchy = $"CheckLayering --LayeringMode {ShellQuote(mode)} --LayeringLayers {string.Join(' ', layout.Layers.Select(ShellQuote))} --LayeringRoot {ShellQuote(layout.Root)}";
        if (layout.Mode == LayeringMode.Folder)
        {
            var mappings = layout.ApplicationFolders.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            var applicationFolders = JsonSerializer.Serialize(mappings);
            cli += $" --layering-library-root {ShellQuote(layout.LibraryRoot!)} --layering-application-folders {ShellQuote(applicationFolders)}";
            anarchy += $" --LayeringLibraryRoot {ShellQuote(layout.LibraryRoot!)} --LayeringApplicationFolders {ShellQuote(applicationFolders)}";
        }
        return (cli, anarchy);
    }

    private static string MakeAnarchyTarget(string arguments) => $"make anarchy TARGET={ShellQuote(arguments)}";

    private static string ShellQuote(string value) => "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";

    private static void SetNested(JsonObject target, string section, string property, JsonNode? value)
    {
        if (target[section] is not JsonObject nested) target[section] = nested = new JsonObject();
        nested[property] = value;
    }

    private bool TryRun(string title, Action<Action<string>> action)
    {
        try
        {
            prompter.RunWithProgress(title, action);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException
                                              or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            prompter.Error($"Failed: {exception.Message}");
            prompter.Error("If generation started, inspect the output before retrying.");
            return false;
        }
    }

    private int Cancelled()
    {
        prompter.Info("Cancelled; no files were created.");
        return 1;
    }

    private int Fail(string message)
    {
        prompter.Error(message);
        return 1;
    }
}
