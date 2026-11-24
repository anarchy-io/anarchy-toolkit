using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Build.Cli.Wizard;

/// <summary>Collects per-application runtime choices without saving preferences.</summary>
internal static class EntryPointWizard
{
    public static ApplicationSpec Ask(IWizardPrompter prompter, ApplicationSpec app)
    {
        if (app.IsWebHost)
        {
            var container = prompter.Confirm("Add a container web entry point?", false);
            var apiLambda = app.Kind == ApplicationKind.Api && prompter.Confirm("Add an API Gateway Lambda entry point?", false);
            return app with { Container = container, ApiLambda = apiLambda };
        }

        var mode = prompter.SelectOne("How should the job run locally?", [JobExecutionMode.Worker, JobExecutionMode.Once],
            value => value == JobExecutionMode.Worker ? "Long-running worker" : "Run once and exit");
        var eventLambda = prompter.Confirm("Add a scheduled Lambda entry point?", false);
        return app with { JobMode = mode, EventLambda = eventLambda };
    }
}
