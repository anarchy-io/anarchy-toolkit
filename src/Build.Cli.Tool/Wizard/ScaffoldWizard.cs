using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>Guides the user through adding an API, job or UI to an existing solution.</summary>
public sealed class ScaffoldWizard(IWizardPrompter prompter)
{
    /// <summary>The confirmed application, or null if the user declined it.</summary>
    public ApplicationSpec? Run(SolutionWorkspace workspace)
    {
        prompter.ShowWelcome("Anarchy application scaffold",
            $"Adds an API, job or UI to {Path.GetFileName(workspace.SolutionFile)}, following its layering.");

        var kinds = SolutionLayout.Kinds.Where(workspace.Layout.Supports).ToArray();
        if (kinds.Length == 0)
        {
            prompter.Error($"The solution has neither a {SolutionLayout.WebLayer} nor a {SolutionLayout.HostingLayer} layer.");
            return null;
        }

        var kind = prompter.SelectOne("What should be added?", kinds, Labels.DescribeKind);
        var name = prompter.Ask($"Name of the {Labels.Of(kind)}", null,
            answer => ApplicationScaffolder.Validate(workspace.Layout, new ApplicationSpec(kind, answer), workspace.ProjectNames));
        var app = new ApplicationSpec(kind, name);
        prompter.ShowScaffold(workspace, app);

        return prompter.Confirm($"Add the {Labels.Of(kind)}?", true) ? app : null;
    }
}
