using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Core.Validation;

namespace Anarchy.Toolkit.Automation.Build.Cli.Wizard;

/// <summary>
/// Guides the user through naming the solution, choosing its directory and layering, adding APIs, jobs and UIs, and
/// optionally installing the agent skills.
/// </summary>
/// <remarks>
/// <see cref="Service.ToolApplication"/> runs it for <c>anarchy-toolkit bootstrap</c> when no solution name is given. Answers
/// are validated as they are entered, so the returned plan has already passed name, directory and layer checks.
/// </remarks>
/// <param name="prompter">The console interactions to run the wizard through.</param>
public sealed class BootstrapWizard(IWizardPrompter prompter)
{
    /// <summary>
    /// Asks for the solution's settings, shows the resulting plan and asks for confirmation.
    /// </summary>
    /// <returns>The confirmed plan, or <c>null</c> if the user declined it.</returns>
    public SolutionPlan? Run()
    {
        prompter.ShowWelcome("Anarchy solution bootstrap",
            "Creates a solution with Anarchy automation (powered by Fallout Build), layered libraries and the APIs, jobs and UIs you choose.");

        var name = prompter.Ask("Solution name", null,
            answer => NameRules.IsValidSolutionName(answer) ? null : NameRules.SolutionNameHelp);
        var directory = prompter.Ask("Output directory", name, SolutionBootstrapper.ValidateRootDirectory);
        var singleApplication = prompter.SelectOne("Will the solution contain one application or several?", [true, false],
            one => one ? "One application (single-app layout)" : "Several applications (monorepo layout)");
        ApplicationKind? singleKind = singleApplication
            ? prompter.SelectOne("What kind of application?", SolutionLayout.Kinds, Labels.DescribeKind)
            : null;
        var applicationFolder = singleApplication
            ? prompter.Ask("Application folder under src", "App", answer =>
                NameRules.IsValidSolutionName(answer) ? null : "Use one folder name made of letters, digits, dots, underscores or hyphens.")
            : null;
        var layout = AskLayout(singleApplication, singleKind, applicationFolder);

        var applications = new List<ApplicationSpec>();
        if (singleKind is { } kind)
            AskApplications(layout, kind, applications, allowMore: false);
        else
        {
            var kinds = SolutionLayout.Kinds.Where(layout.Supports).ToArray();
            var selected = prompter.SelectMany("Which applications should the solution start with?", kinds, Labels.DescribeKind);
            foreach (var selectedKind in kinds.Where(selected.Contains))
                AskApplications(layout, selectedKind, applications);
        }

        var skills = prompter.Confirm("Install the toolkit's agent skills (.agents/skills) for AI coding agents?", false);

        var plan = new SolutionPlan(name, Path.GetFullPath(directory), layout, applications) { AgentSkills = skills };
        prompter.ShowPlan(plan);
        var savePreferences = prompter.Confirm("Save the selected layering preferences in .anarchy-toolkit.json?", false);
        plan = plan with { SavePreferences = savePreferences };

        return prompter.Confirm("Create the solution?", true) ? plan : null;
    }

    private SolutionLayout AskLayout(bool singleApplication, ApplicationKind? kind, string? applicationFolder)
    {
        var mode = prompter.SelectOne("How should the layering check find each project's layer?",
            [LayeringMode.Folder, LayeringMode.Prefix], Labels.DescribeMode);
        var preset = singleApplication
            ? SolutionLayoutPresets.Create(SolutionLayoutPreset.SingleApp, kind!.Value, applicationFolder!)
            : SolutionLayoutPresets.Create(SolutionLayoutPreset.Monorepo);
        var layers = prompter.Ask("Layers, innermost first", string.Join(", ", preset.Layers), answer => {
            var parsed = ParseLayers(answer);
            if (NameRules.ValidateLayers(parsed) is { } invalid) return invalid;
            return singleApplication && !parsed.Contains(SolutionLayout.LayerOf(kind!.Value), StringComparer.Ordinal)
                ? $"The {Labels.Of(kind.Value)} application needs the {SolutionLayout.LayerOf(kind.Value)} layer."
                : null;
        });

        var selectedLayers = ParseLayers(layers);
        return mode == LayeringMode.Prefix
            ? new SolutionLayout(mode, selectedLayers) { Root = preset.Root }
            : preset with {
                Mode = mode,
                Layers = selectedLayers,
                ApplicationFolders = singleApplication
                    ? preset.ApplicationFolders
                    : SolutionLayout.Kinds.Where(applicationKind => selectedLayers.Contains(SolutionLayout.LayerOf(applicationKind)))
                        .ToDictionary(applicationKind => $"{preset.Root}/{SolutionLayout.FolderOf(applicationKind)}", SolutionLayout.LayerOf)
            };
    }

    private static string[] ParseLayers(string answer) =>
        answer.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private void AskApplications(SolutionLayout layout, ApplicationKind kind, List<ApplicationSpec> applications, bool allowMore = true)
    {
        var number = 1;
        do
        {
            var taken = layout.Layers.Concat(applications.Select(layout.ProjectName)).ToArray();
            var appName = prompter.Ask($"Name of {Labels.Of(kind)} #{number}", null,
                answer => ApplicationScaffolder.Validate(layout, new ApplicationSpec(kind, answer), taken));
            applications.Add(new ApplicationSpec(kind, appName));
            number++;
        } while (allowMore && prompter.Confirm($"Add another {Labels.Of(kind)}?", false));
    }
}
