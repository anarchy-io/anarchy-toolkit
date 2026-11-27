using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;

namespace Anarchy.Toolkit.Automation.Build.Cli.Wizard;

/// <summary>
/// Guides the user through adding an API, job or UI to an existing solution.
/// </summary>
/// <remarks>
/// <see cref="Service.ToolApplication"/> runs it for <c>anarchy-toolkit scaffold</c> when no application is given.
/// </remarks>
/// <param name="prompter">The console interactions to run the wizard through.</param>
public sealed class ScaffoldWizard(IWizardPrompter prompter)
{
    /// <summary>Whether the user opted to save the Swagger choices made in this wizard.</summary>
    public bool SaveSwaggerPreferences { get; private set; }

    /// <summary>
    /// Asks for the kind and name of the application, shows the project to add and asks for confirmation.
    /// </summary>
    /// <param name="workspace">The solution to add to; only the kinds its layering supports are offered.</param>
    /// <param name="defaultSwaggerTheme">The saved Swagger-theme override, or the toolkit default.</param>
    /// <param name="defaultSwagger">The saved Swagger override, or the toolkit default.</param>
    /// <returns>
    /// The confirmed application, or <c>null</c> if the user declined it or the solution supports no application kind.
    /// </returns>
    public ApplicationSpec? Run(SolutionWorkspace workspace, bool defaultSwaggerTheme = true, bool defaultSwagger = true)
    {
        prompter.ShowWelcome("Anarchy application scaffold",
            $"Adds an API, job or UI to {Path.GetFileName(workspace.SolutionFile)}, following its layering.");

        var kinds = SolutionLayout.Kinds.Where(workspace.Layout.Supports).ToArray();
        if (kinds.Length == 0)
        {
            prompter.Error(workspace.Layout.Mode == LayeringMode.Folder &&
                           SolutionLayout.Kinds.Any(kind => workspace.Layout.Layers.Contains(SolutionLayout.LayerOf(kind)))
                ? workspace.Layout.MissingApplicationFolder(SolutionLayout.Kinds.First(kind => workspace.Layout.Layers.Contains(SolutionLayout.LayerOf(kind))))
                : $"The solution has no {SolutionLayout.AppsLayer} layer.");
            return null;
        }

        var kind = prompter.SelectOne("What should be added?", kinds, Labels.DescribeKind);
        var name = prompter.Ask($"Name of the {Labels.Of(kind)}", null,
            answer => ApplicationScaffolder.Validate(workspace.Layout, new ApplicationSpec(kind, answer), workspace.ProjectNames));
        var swagger = kind != ApplicationKind.Api || prompter.Confirm("Include Swagger?", defaultSwagger);
        var swaggerTheme = kind == ApplicationKind.Api && swagger
            ? prompter.Confirm("Use the toolkit's Swagger UI theme?", defaultSwaggerTheme)
            : defaultSwaggerTheme;
        var app = EntryPointWizard.Ask(prompter, new ApplicationSpec(kind, name, swaggerTheme) { Swagger = swagger });
        prompter.ShowScaffold(workspace, app);

        if (!prompter.Confirm($"Add the {Labels.Of(kind)}?", true)) return null;
        if (kind == ApplicationKind.Api)
            SaveSwaggerPreferences = prompter.Confirm(swagger ? "Save these Swagger preferences?" : "Save this Swagger preference?", false);
        return app;
    }
}
