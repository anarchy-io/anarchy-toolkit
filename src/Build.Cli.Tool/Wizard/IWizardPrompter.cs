using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>
/// The console interactions of the tool, abstracted so the wizards and <see cref="Service.ToolApplication"/> can run
/// against any front end (including test doubles).
/// </summary>
public interface IWizardPrompter
{
    /// <summary>
    /// Shows Anarchy's logo before a command runs. Implementations may skip it when output is redirected.
    /// </summary>
    void ShowBanner();

    /// <summary>
    /// Introduces a wizard with a <paramref name="title"/> and a one-line <paramref name="description"/>.
    /// </summary>
    void ShowWelcome(string title, string description);

    /// <summary>
    /// Shows the command-line usage.
    /// </summary>
    void ShowHelp();

    /// <summary>
    /// Asks for a text answer until <paramref name="validate"/> accepts it.
    /// </summary>
    /// <param name="prompt">The question, without a trailing colon.</param>
    /// <param name="defaultValue">The answer used when the user enters nothing, or <c>null</c> for none.</param>
    /// <param name="validate">
    /// Receives the trimmed answer and returns <c>null</c> to accept it, or an error message to show before asking again.
    /// </param>
    /// <returns>The accepted, trimmed answer.</returns>
    string Ask(string prompt, string? defaultValue, Func<string, string?> validate);

    /// <summary>
    /// Asks a yes/no question, answering <paramref name="defaultValue"/> when the user just presses Enter.
    /// </summary>
    bool Confirm(string prompt, bool defaultValue);

    /// <summary>
    /// Lets the user pick one of <paramref name="choices"/>, shown through <paramref name="label"/>; the first is
    /// preselected.
    /// </summary>
    T SelectOne<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull;

    /// <summary>
    /// Lets the user pick any number of <paramref name="choices"/>, including none, shown through
    /// <paramref name="label"/>.
    /// </summary>
    IReadOnlyList<T> SelectMany<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull;

    /// <summary>
    /// Shows the solution about to be bootstrapped: its directory, layering and projects.
    /// </summary>
    void ShowPlan(SolutionPlan plan);

    /// <summary>
    /// Shows the project about to be added to <paramref name="workspace"/> for <paramref name="app"/>.
    /// </summary>
    void ShowScaffold(SolutionWorkspace workspace, ApplicationSpec app);

    /// <summary>
    /// Runs <paramref name="action"/>, displaying the progress messages it reports through the callback it receives.
    /// </summary>
    /// <param name="title">The status shown before the first progress message.</param>
    /// <param name="action">The work to run; exceptions it throws propagate to the caller.</param>
    void RunWithProgress(string title, Action<Action<string>> action);

    /// <summary>
    /// Shows the commands to run next, after the work described by <paramref name="title"/> is done.
    /// </summary>
    /// <param name="title">What was completed, e.g. <c>MySolution is ready</c>.</param>
    /// <param name="steps">The commands to run, in order.</param>
    /// <param name="note">An optional remark shown after the steps, or <c>null</c> for none.</param>
    void ShowNextSteps(string title, IReadOnlyList<string> steps, string? note);

    /// <summary>
    /// Shows an informational message.
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Shows an error message.
    /// </summary>
    void Error(string message);
}
