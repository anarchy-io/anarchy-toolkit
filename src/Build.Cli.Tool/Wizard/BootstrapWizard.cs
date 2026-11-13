using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Cli.Core.Validation;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>
/// Guides the user through naming the solution, choosing its directory and layering, and adding APIs, jobs and UIs.
/// </summary>
public sealed class BootstrapWizard(IWizardPrompter prompter)
{
    /// <summary>The confirmed plan, or null if the user declined it.</summary>
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

        var plan = new SolutionPlan(name, Path.GetFullPath(directory), layout, applications);
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
