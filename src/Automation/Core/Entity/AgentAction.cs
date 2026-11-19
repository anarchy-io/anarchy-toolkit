namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>
/// A documented Make target (<c>name: #? description</c>), with the agent skill it belongs to when a <c>#@</c> comment
/// requires one. <see cref="Service.AgentSkillTasks"/> lists it in that skill's commands.
/// </summary>
/// <param name="Name">The target, e.g. <c>check_layering</c>.</param>
/// <param name="Description">The target's <c>#?</c> text.</param>
/// <param name="File">The name of the <c>.mk</c> file declaring it, without extension.</param>
/// <param name="Skill">The skill that must describe it, or <see langword="null"/> when none is required.</param>
public sealed record AgentAction(string Name, string Description, string File, string? Skill)
{
    /// <summary>
    /// Other names of the same target (e.g. <c>restore</c> for <c>install_dependencies</c>).
    /// </summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>
    /// The Make variables documented with <c>#+ NAME|type|description</c>.
    /// </summary>
    public IReadOnlyList<AgentActionParameter> Parameters { get; init; } = [];

    /// <summary>
    /// The warnings documented with <c>#!</c>.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// A Make variable an <see cref="AgentAction"/> accepts.
/// </summary>
/// <param name="Name">The variable, e.g. <c>APP_NAME</c>.</param>
/// <param name="Type">Its documented type, e.g. <c>str</c> or <c>bool</c>.</param>
/// <param name="Description">What it does.</param>
public sealed record AgentActionParameter(string Name, string Type, string Description);
