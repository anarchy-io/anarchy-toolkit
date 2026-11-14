using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Cli.Core.Service;
using Anarchy.Toolkit.Build.Cli.Tool.Wizard;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Tool;

/// <summary>Answers the wizards from a script and records what they showed.</summary>
internal sealed class ScriptedPrompter(params object[] answers) : IWizardPrompter
{
    private readonly Queue<object> _answers = new(answers);

    public List<string> ValidationErrors { get; } = [];
    public List<string> Messages { get; } = [];
    public List<string> Errors { get; } = [];
    public List<string> Progress { get; } = [];
    public List<string> NextSteps { get; } = [];
    public SolutionPlan? ShownPlan { get; private set; }
    public ApplicationSpec? ShownScaffold { get; private set; }
    public bool ShowedHelp { get; private set; }
    public int BannersShown { get; private set; }

    public void ShowBanner() => BannersShown++;

    public void ShowWelcome(string title, string description) { }

    public void ShowHelp() => ShowedHelp = true;

    public string Ask(string prompt, string? defaultValue, Func<string, string?> validate)
    {
        while (true)
        {
            var answer = (string)_answers.Dequeue();
            if (answer.Length == 0 && defaultValue is not null) answer = defaultValue;
            if (validate(answer) is not { } error) return answer;
            ValidationErrors.Add(error);
        }
    }

    public bool Confirm(string prompt, bool defaultValue) => (bool)_answers.Dequeue();

    public T SelectOne<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull
    {
        var answer = (T)_answers.Dequeue();
        if (!choices.Contains(answer)) throw new InvalidOperationException($"{answer} was not offered.");
        return answer;
    }

    public IReadOnlyList<T> SelectMany<T>(string title, IReadOnlyList<T> choices, Func<T, string> label) where T : notnull =>
        ((IEnumerable<object>)_answers.Dequeue()).Cast<T>().ToArray();

    public void ShowPlan(SolutionPlan plan) => ShownPlan = plan;

    public void ShowScaffold(SolutionWorkspace workspace, ApplicationSpec app) => ShownScaffold = app;

    public void RunWithProgress(string title, Action<Action<string>> action) => action(Progress.Add);

    public void ShowNextSteps(string title, IReadOnlyList<string> steps, string? note) => NextSteps.AddRange(steps);

    public void Info(string message) => Messages.Add(message);

    public void Error(string message) => Errors.Add(message);
}
