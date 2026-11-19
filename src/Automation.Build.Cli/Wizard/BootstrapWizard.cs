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
            "Creates a solution with a Fallout build, layered libraries and the APIs, jobs and UIs you choose.");

        var name = prompter.Ask("Solution name", null,
            answer => NameRules.IsValidSolutionName(answer) ? null : NameRules.SolutionNameHelp);
        var directory = prompter.Ask("Output directory", name, SolutionBootstrapper.ValidateRootDirectory);
        var layout = AskLayout();

        var applications = new List<ApplicationSpec>();
        var kinds = SolutionLayout.Kinds.Where(layout.Supports).ToArray();
        var selected = prompter.SelectMany("Which applications should the solution start with?", kinds, Labels.DescribeKind);
        foreach (var kind in kinds.Where(selected.Contains))
            AskApplications(layout, kind, applications);

        var skills = prompter.Confirm("Install the toolkit's agent skills (.agents/skills) for AI coding agents?", false);

        var plan = new SolutionPlan(name, Path.GetFullPath(directory), layout, applications) { AgentSkills = skills };
        prompter.ShowPlan(plan);

        return prompter.Confirm("Create the solution?", true) ? plan : null;
    }

    private SolutionLayout AskLayout()
    {
        var mode = prompter.SelectOne("How should the layering check find each project's layer?",
            [LayeringMode.Folder, LayeringMode.Prefix], Labels.DescribeMode);
        var layers = prompter.Ask("Layers, innermost first", string.Join(", ", SolutionLayout.DefaultLayers),
            answer => NameRules.ValidateLayers(ParseLayers(answer)));

        return new SolutionLayout(mode, ParseLayers(layers));
    }

    private static string[] ParseLayers(string answer) =>
        answer.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private void AskApplications(SolutionLayout layout, ApplicationKind kind, List<ApplicationSpec> applications)
    {
        var number = 1;
        do
        {
            var taken = layout.Layers.Concat(applications.Select(layout.ProjectName)).ToArray();
            var appName = prompter.Ask($"Name of {Labels.Of(kind)} #{number}", null,
                answer => ApplicationScaffolder.Validate(layout, new ApplicationSpec(kind, answer), taken));
            applications.Add(new ApplicationSpec(kind, appName));
            number++;
        } while (prompter.Confirm($"Add another {Labels.Of(kind)}?", false));
    }
}
