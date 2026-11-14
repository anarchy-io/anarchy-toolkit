using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Cli.Core.Validation;
using Anarchy.Toolkit.Build.Cli.Tool.Command;
using Anarchy.Toolkit.Build.Cli.Tool.Wizard;
using Spectre.Console;

namespace Anarchy.Toolkit.Build.Cli.Tool.Service;

/// <summary>
/// Runs the tool: parses the command line (showing the help without a command), runs a wizard when needed, and
/// bootstraps or scaffolds. Bootstrapping defaults to the working directory; scaffolding looks for a solution from the
/// working directory upwards.
/// </summary>
public sealed class ToolApplication(IWizardPrompter prompter, ToolkitPackageVersions versions, string workingDirectory)
{
    private const string ReviewSettings = "Review each application's settings (auth service URLs, client IDs) before running it.";

    /// <summary>
    /// Creates the application for the real console, the package versions embedded in this assembly and the current
    /// directory.
    /// </summary>
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
        if (command.Mode != ToolMode.Invalid) prompter.ShowBanner();
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

        var workspace = SolutionWorkspace.Find(workingDirectory);
        return command.Mode switch {
            ToolMode.Bootstrap => Bootstrap(command),
            _ when workspace is null => Fail("No Anarchy solution (.fallout/parameters.json with a Solution) found here or above; run 'anarchy-toolkit bootstrap' first."),
            _ => Scaffold(workspace, command.Application)
        };
    }

    private int Bootstrap(ToolCommand command)
    {
        SolutionPlan? plan;
        if (command.SolutionName is null)
        {
            plan = new BootstrapWizard(prompter).Run();
            if (plan is null) return Cancelled();
        }
        else if (NameRules.IsValidSolutionName(command.SolutionName))
        {
            plan = new SolutionPlan(command.SolutionName, Path.GetFullPath(command.OutputDirectory ?? workingDirectory, workingDirectory));
        }
        else
        {
            return Fail(NameRules.SolutionNameHelp);
        }

        if (SolutionBootstrapper.Validate(plan) is { } invalid) return Fail(invalid);
        if (!TryRun($"Creating {plan.Name}", progress => SolutionBootstrapper.Create(plan, versions, progress))) return 1;

        var steps = new List<string> {
            $"cd {plan.RootDirectory}",
            "make compile",
            "make check_layering"
        };
        steps.AddRange(plan.Applications.Where(app => app.IsWebHost).Select(app => $"dotnet run --project {plan.Layout.ProjectPath(app)}"));
        steps.Add("anarchy-toolkit scaffold    # add more APIs, jobs or UIs later");
        prompter.ShowNextSteps($"{plan.Name} is ready", steps, plan.Applications.Count > 0 ? ReviewSettings : null);
        return 0;
    }

    private int Scaffold(SolutionWorkspace workspace, ApplicationSpec? app)
    {
        app ??= new ScaffoldWizard(prompter).Run(workspace);
        if (app is null) return Cancelled();
        if (ApplicationScaffolder.Validate(workspace.Layout, app, workspace.ProjectNames) is { } invalid) return Fail(invalid);
        if (!TryRun($"Adding {app.Name}", progress => ApplicationScaffolder.Add(workspace, app, versions, progress))) return 1;

        var path = workspace.Layout.ProjectPath(app);
        var steps = new List<string> { "make compile", "make check_layering" };
        if (app.IsWebHost) steps.Add($"dotnet run --project {path}");
        prompter.ShowNextSteps($"{path} is ready", steps, ReviewSettings);
        return 0;
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
