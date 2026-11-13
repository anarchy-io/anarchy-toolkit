using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>The console interactions of the tool, so the wizards can run against any front end.</summary>
public interface IWizardPrompter
{
    void ShowWelcome(string title, string description);

    /// <summary>Shows the command-line usage.</summary>
    void ShowHelp();

    /// <summary>Asks until <paramref name="validate"/> accepts the trimmed answer (returns null); an empty answer takes the default.</summary>
    string Ask(string prompt, string? defaultValue, Func<string, string?> validate);

    bool Confirm(string prompt, bool defaultValue);

    /// <summary>Lets the user pick one choice; the first is the default.</summary>
    T SelectOne<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull;

    /// <summary>Lets the user pick any number of choices, including none.</summary>
    IReadOnlyList<T> SelectMany<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull;

    void ShowPlan(SolutionPlan plan);

    void ShowScaffold(SolutionWorkspace workspace, ApplicationSpec app);

    /// <summary>Runs <paramref name="action"/>, displaying the progress messages it reports.</summary>
    void RunWithProgress(string title, Action<Action<string>> action);

    /// <summary>Shows what to do next, after <paramref name="title"/> is done.</summary>
    void ShowNextSteps(string title, IReadOnlyList<string> steps, string? note);

    void Info(string message);

    void Error(string message);
}
