using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Cli.Core.Validation;
using Anarchy.Toolkit.Build.Cli.Tool.Command;
using Anarchy.Toolkit.Build.Cli.Tool.Wizard;
using Spectre.Console;

namespace Anarchy.Toolkit.Build.Cli.Tool.Service;

/// <summary>
/// Runs the tool: parses the command line (showing the help without a command), runs a wizard when needed, and
/// bootstraps, scaffolds or installs the agent skills. Bootstrapping defaults to the working directory; scaffolding
/// and installing the skills look for a solution from the working directory upwards.
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
            ToolMode.Skills => InstallSkills(command.OutputDirectory is { } output
                ? Path.GetFullPath(output, workingDirectory)
                : workspace?.Root ?? workingDirectory),
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
            plan = new SolutionPlan(command.SolutionName, Path.GetFullPath(command.OutputDirectory ?? workingDirectory, workingDirectory)) {
                AgentSkills = command.AgentSkills
            };
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
                    var root = SolutionWorkspace.Find(workingDirectory)?.Root ?? workingDirectory;
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
        if (!TryRun("Installing the agent skills", progress =>
                SolutionScaffolder.Create(root, null, new ScaffoldOptions { Items = "skills", Log = progress })))
            return 1;

        prompter.ShowNextSteps($"Agent skills installed in {Path.Combine(root, AgentSkillTasks.DefaultDirectory)}", [
            "Commit .agents/skills so every agent working on the solution can use them",
            "make skills_validate    # check that the skills list the current make targets",
            "make skills_sync        # refresh the skills' commands, and create skills for #@-marked targets"
        ], null);
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
