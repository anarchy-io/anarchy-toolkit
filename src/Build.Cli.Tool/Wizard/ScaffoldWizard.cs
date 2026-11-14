using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>
/// Guides the user through adding an API, job or UI to an existing solution.
/// </summary>
public sealed class ScaffoldWizard(IWizardPrompter prompter)
{
    /// <summary>
    /// Asks for the kind and name of the application, shows the project to add and asks for confirmation.
    /// </summary>
    /// <param name="workspace">The solution to add to; only the kinds its layering supports are offered.</param>
    /// <returns>
    /// The confirmed application, or <c>null</c> if the user declined it or the solution supports no application kind.
    /// </returns>
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
