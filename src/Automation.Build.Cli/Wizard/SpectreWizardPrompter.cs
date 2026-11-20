using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;
using Anarchy.Toolkit.Core.Helper;
using Spectre.Console;

namespace Anarchy.Toolkit.Automation.Build.Cli.Wizard;

/// <summary>
/// An <see cref="IWizardPrompter"/> on Spectre.Console: validated prompts, selection lists, tables and spinners.
/// User-supplied text is markup-escaped. The banner is skipped when output is redirected, and progress falls back to
/// plain lines when the console is not interactive.
/// </summary>
/// <remarks>
/// This is the prompter used by <see cref="Service.ToolApplication.CreateDefault"/>. Tests typically substitute their own
/// <see cref="IWizardPrompter"/> instead, or pass a Spectre.Console test console to this class.
/// </remarks>
/// <param name="console">The Spectre.Console console all prompts and output go to.</param>
/// <example>
/// <code language="csharp">
/// var prompter = new SpectreWizardPrompter(Spectre.Console.AnsiConsole.Console);
/// var plan = new BootstrapWizard(prompter).Run();
/// </code>
/// </example>
public sealed class SpectreWizardPrompter(IAnsiConsole console) : IWizardPrompter
{
    /// <inheritdoc/>
    public void ShowBanner() => new ConsoleBannerOptions().Write(console);

    /// <inheritdoc/>
    public void ShowWelcome(string title, string description)
    {
        console.Write(new Rule($"[bold yellow]{Markup.Escape(title)}[/]").LeftJustified());
        console.MarkupLine($"[grey]{Markup.Escape(description)}[/]");
        console.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowHelp()
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap().PadRight(4)).AddColumn();
        grid.AddRow("[blue]anarchy-toolkit bootstrap[/]", "Guided setup: name, layering, and the APIs, jobs and UIs to start with.");
        grid.AddRow("[blue]anarchy-toolkit bootstrap <name> [[--output <dir>]] [[--with-skills]] [[layout options]] [[--save-preferences]][/]", "Create a solution; preferences are not saved unless requested.");
        grid.AddRow("[blue]anarchy-toolkit scaffold[/]", "Guided addition of an API, job or UI to the solution here.");
        grid.AddRow("[blue]anarchy-toolkit scaffold api|job|ui <Name>[/]", "Add one without prompts.");
        grid.AddRow("[blue]anarchy-toolkit scaffold api <Name> --no-swagger-theme[/]", "Add an API whose Swagger UI keeps its own look.");
        grid.AddRow("[blue]anarchy-toolkit scaffold api <Name> --swagger-theme[/]", "Explicitly enable the toolkit's Swagger UI theme.");
        grid.AddRow("[blue]layout options[/]", "--layering folder|prefix, --layering-layers (JSON array), --layering-root <path>, --layering-library-root <path>, --layering-application-folders (JSON object).");
        grid.AddRow("[blue]anarchy-toolkit config show|set|unset|configure[/]", "Show, edit or interactively configure optional solution preferences.");
        grid.AddRow("[blue]anarchy-toolkit skills install [[--output <dir>]][/]", "Install the agent skills into the first configured agentSkills.directories destination.");
        grid.AddRow("[blue]anarchy-toolkit boilerplate list[/]", "List the packaged boilerplate files (Makefile, scripts, settings, skills).");
        grid.AddRow("[blue]anarchy-toolkit boilerplate show <path>[/]", "Print a packaged file without installing it.");
        grid.AddRow("[blue]anarchy-toolkit boilerplate diff [[<path>...]][/]", "Diff the solution's copies against the packaged files (exit code 1 when any differ).");
        grid.AddRow("[blue]anarchy-toolkit [[--help]][/]", "Show this help.");

        console.Write(new Panel(grid)
            .Header("[bold yellow]Anarchy toolkit[/]")
            .Border(BoxBorder.Rounded));
        console.MarkupLine("[grey]Existing generated files are never overwritten. Configuration edits are explicit; bootstrap/scaffold do not save preferences unless requested. The .NET 10 SDK is required.[/]");
    }

    /// <inheritdoc/>
    public string Ask(string prompt, string? defaultValue, Func<string, string?> validate)
    {
        var textPrompt = new TextPrompt<string>(Markup.Escape(prompt) + ":")
            .Validate(answer => validate(answer.Trim()) is { } error
                ? ValidationResult.Error("[red]" + Markup.Escape(error) + "[/]")
                : ValidationResult.Success());
        if (defaultValue is not null) textPrompt.DefaultValue(defaultValue);

        return console.Prompt(textPrompt).Trim();
    }

    /// <inheritdoc/>
    public bool Confirm(string prompt, bool defaultValue) => console.Confirm(Markup.Escape(prompt), defaultValue);

    /// <inheritdoc/>
    public T SelectOne<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull =>
        console.Prompt(new SelectionPrompt<T>()
            .Title(Markup.Escape(title))
            .UseConverter(choice => Markup.Escape(label(choice)))
            .AddChoices(choices));

    /// <inheritdoc/>
    public IReadOnlyList<T> SelectMany<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull =>
        console.Prompt(new MultiSelectionPrompt<T>()
            .Title(Markup.Escape(title))
            .NotRequired()
            .InstructionsText("[grey](Space to toggle, Enter to accept; select none for a baseline solution)[/]")
            .UseConverter(choice => Markup.Escape(label(choice)))
            .AddChoices(choices));

    /// <inheritdoc/>
    public void ShowPlan(SolutionPlan plan)
    {
        console.WriteLine();
        console.MarkupLine($"[bold]{Markup.Escape(plan.Name)}[/] in [blue]{Markup.Escape(plan.RootDirectory)}[/]");
        console.MarkupLine($"[grey]Layering by {(plan.Layout.Mode == LayeringMode.Folder ? "folder" : "name prefix")}: {Markup.Escape(string.Join(" < ", plan.Layout.Layers))}[/]");

        var table = NewProjectTable();
        table.AddRow("build", "build/_build.csproj", "", "");
        foreach (var layer in plan.Layout.Layers)
            table.AddRow("library", Markup.Escape(plan.Layout.LibraryProjectPath(layer)), Markup.Escape(layer), "");
        var ports = plan.Ports;
        foreach (var app in plan.Applications)
            AddApplicationRow(table, plan.Layout, app, ports.TryGetValue(app, out var port) ? port : null);
        console.Write(table);
        if (plan.AgentSkills) console.MarkupLine("[grey]With the toolkit's agent skills in .agents/skills[/]");
    }

    /// <inheritdoc/>
    public void ShowScaffold(SolutionWorkspace workspace, ApplicationSpec app)
    {
        console.WriteLine();
        var table = NewProjectTable();
        var ports = LocalPorts.Assign(app.IsWebHost ? [app] : Array.Empty<ApplicationSpec>(), workspace.UsedPorts);
        AddApplicationRow(table, workspace.Layout, app, ports.TryGetValue(app, out var port) ? port : null);
        console.Write(table);
    }

    private static Table NewProjectTable() => new Table().Border(TableBorder.Rounded).AddColumns("Kind", "Project", "Layer", "Local URLs");

    private static void AddApplicationRow(Table table, SolutionLayout layout, ApplicationSpec app, (int Http, int Https)? port) =>
        table.AddRow(
            Labels.Of(app.Kind),
            Markup.Escape(layout.ProjectPath(app)),
            SolutionLayout.LayerOf(app.Kind),
            port is { } p ? $"http://localhost:{p.Http}, https://localhost:{p.Https}" : "");

    /// <inheritdoc/>
    public void RunWithProgress(string title, Action<Action<string>> action)
    {
        string? current = null;
        void Complete()
        {
            if (current is not null) console.MarkupLine($"[green]✓[/] {Markup.Escape(current)}");
        }

        if (!console.Profile.Capabilities.Interactive)
        {
            action(message => {
                Complete();
                current = message;
            });
            Complete();
            return;
        }

        console.Status().Spinner(Spinner.Known.Dots).Start(Markup.Escape(title), context => action(message => {
            Complete();
            current = message;
            context.Status(Markup.Escape(message) + "...");
        }));
        Complete();
    }

    /// <inheritdoc/>
    public void ShowNextSteps(string title, IReadOnlyList<string> steps, string? note)
    {
        console.WriteLine();
        console.Write(new Panel(new Markup(string.Join("\n", steps.Select(step => "[blue]" + Markup.Escape(step) + "[/]"))))
            .Header($"[green]{Markup.Escape(title)}[/] - next steps")
            .Border(BoxBorder.Rounded));
        if (note is not null) console.MarkupLine($"[grey]{Markup.Escape(note)}[/]");
    }

    /// <inheritdoc/>
    public void Info(string message) => console.MarkupLine(Markup.Escape(message));

    /// <inheritdoc/>
    public void Error(string message) => console.MarkupLine("[red]" + Markup.Escape(message) + "[/]");
}
