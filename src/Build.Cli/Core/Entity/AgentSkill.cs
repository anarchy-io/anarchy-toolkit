namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// An agent skill: a <c>SKILL.md</c> file whose front matter names it and describes when to use it.
/// </summary>
/// <param name="Path">The <c>SKILL.md</c> file.</param>
/// <param name="Name">The skill name, which matches its directory.</param>
/// <param name="Description">What the skill does and when to use it.</param>
/// <param name="Content">The whole file.</param>
public sealed record AgentSkill(string Path, string Name, string Description, string Content);
