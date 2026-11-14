using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;
using Spectre.Console;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>
/// An <see cref="IWizardPrompter"/> on Spectre.Console: validated prompts, selection lists, tables and spinners.
/// User-supplied text is markup-escaped. The banner is skipped when output is redirected, and progress falls back to
/// plain lines when the console is not interactive.
/// </summary>
public sealed class SpectreWizardPrompter(IAnsiConsole console) : IWizardPrompter
{
    public void ShowBanner()
    {
        // Kept out of redirected output, so piped or captured output stays plain.
        if (Console.IsOutputRedirected) return;
        var writer = console.Profile.Out.Writer;
        writer.Write(ConsoleBanner.Render(console.Profile.Width, color: console.Profile.Capabilities.Ansi));
        writer.Flush();
    }

    public void ShowWelcome(string title, string description)
    {
        console.Write(new Rule($"[bold yellow]{Markup.Escape(title)}[/]").LeftJustified());
        console.MarkupLine($"[grey]{Markup.Escape(description)}[/]");
        console.WriteLine();
    }

    public void ShowHelp()
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap().PadRight(4)).AddColumn();
        grid.AddRow("[blue]anarchy-toolkit bootstrap[/]", "Guided setup: name, layering, and the APIs, jobs and UIs to start with.");
        grid.AddRow("[blue]anarchy-toolkit bootstrap <name> [[--output <dir>]][/]", "Create the baseline solution in an empty or new directory (default: current).");
        grid.AddRow("[blue]anarchy-toolkit scaffold[/]", "Guided addition of an API, job or UI to the solution here.");
        grid.AddRow("[blue]anarchy-toolkit scaffold api|job|ui <Name>[/]", "Add one without prompts.");
        grid.AddRow("[blue]anarchy-toolkit [[--help]][/]", "Show this help.");

        console.Write(new Panel(grid)
            .Header("[bold yellow]Anarchy toolkit[/]")
            .Border(BoxBorder.Rounded));
        console.MarkupLine("[grey]Existing files are never overwritten. Package versions follow the solution's Directory.Packages.props when it manages them centrally. The .NET 10 SDK is required.[/]");
    }

    public string Ask(string prompt, string? defaultValue, Func<string, string?> validate)
    {
        var textPrompt = new TextPrompt<string>(Markup.Escape(prompt) + ":")
            .Validate(answer => validate(answer.Trim()) is { } error
                ? ValidationResult.Error("[red]" + Markup.Escape(error) + "[/]")
                : ValidationResult.Success());
        if (defaultValue is not null) textPrompt.DefaultValue(defaultValue);

        return console.Prompt(textPrompt).Trim();
    }

    public bool Confirm(string prompt, bool defaultValue) => console.Confirm(Markup.Escape(prompt), defaultValue);

    public T SelectOne<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull =>
        console.Prompt(new SelectionPrompt<T>()
            .Title(Markup.Escape(title))
            .UseConverter(choice => Markup.Escape(label(choice)))
            .AddChoices(choices));

    public IReadOnlyList<T> SelectMany<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull =>
        console.Prompt(new MultiSelectionPrompt<T>()
            .Title(Markup.Escape(title))
            .NotRequired()
            .InstructionsText("[grey](Space to toggle, Enter to accept; select none for a baseline solution)[/]")
            .UseConverter(choice => Markup.Escape(label(choice)))
            .AddChoices(choices));

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
    }

    public void ShowScaffold(SolutionWorkspace workspace, ApplicationSpec app)
    {
        console.WriteLine();
        var table = NewProjectTable();
        var ports = LocalPorts.Assign([app], workspace.UsedPorts);
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

    public void ShowNextSteps(string title, IReadOnlyList<string> steps, string? note)
    {
        console.WriteLine();
        console.Write(new Panel(new Markup(string.Join("\n", steps.Select(step => "[blue]" + Markup.Escape(step) + "[/]"))))
            .Header($"[green]{Markup.Escape(title)}[/] - next steps")
            .Border(BoxBorder.Rounded));
        if (note is not null) console.MarkupLine($"[grey]{Markup.Escape(note)}[/]");
    }

    public void Info(string message) => console.MarkupLine(Markup.Escape(message));

    public void Error(string message) => console.MarkupLine("[red]" + Markup.Escape(message) + "[/]");
}
