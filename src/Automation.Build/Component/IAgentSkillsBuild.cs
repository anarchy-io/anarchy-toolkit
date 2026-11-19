using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Fallout.Common;
using Serilog;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Keeps the solution's agent skills in step with its Makefiles: every documented Make target marked with <c>#@</c>
/// must be listed by an agent skill under <see cref="AgentSkillsDirectories"/>. See <see cref="AgentSkillTasks"/> for
/// the markers and the skill format.
/// </summary>
/// <remarks>
/// The toolkit's own targets are marked for its skills (<c>anarchy</c>, <c>anarchy-develop</c>,
/// <c>anarchy-environment</c>, <c>anarchy-release</c> and <c>anarchy-maintenance</c>), installed with
/// <c>make install_skills</c> or <c>anarchy-toolkit skills install</c>. <see cref="ValidateAgentSkills"/> is not part
/// of <see cref="IStandardBuild.PullRequest"/>: run it as a separate pull request check.
/// </remarks>
/// <example>
/// <code language="csharp">
/// partial class Build : FalloutBuild, IAgentSkillsBuild
/// {
///     string IAgentSkillsBuild.AgentSkillsPrefix => "shop";
/// }
/// </code>
/// Or, without code: <c>fallout validate-agent-skills --agent-skills-prefix shop</c>.
/// </example>
public interface IAgentSkillsBuild : IStandardBuild
{
    /// <summary>
    /// The prefix of skills named by a bare <c>#@</c> (<c>&lt;prefix&gt;-&lt;file&gt;</c>); defaults to the solution's
    /// name (<c>Shop.Api.sln</c> gives <c>shop-api</c>).
    /// </summary>
    [Parameter("Prefix of agent skills named by a bare #@ marker (defaults to the solution's name)")]
    string AgentSkillsPrefix => TryGetValue(() => AgentSkillsPrefix) ?? AgentSkillTasks.ToSkillName(Solution.Name ?? "solution");

    /// <summary>
    /// The skill directories, relative to the root directory; new skills are created in the first. Defaults to
    /// <c>.agents/skills</c>.
    /// </summary>
    [Parameter("Agent skill directories, relative to the root; new skills are created in the first (defaults to .agents/skills)")]
    string[] AgentSkillsDirectories => TryGetValue(() => AgentSkillsDirectories) ?? [AgentSkillTasks.DefaultDirectory];

    /// <summary>
    /// Fails when a skill required by a <c>#@</c> marker is missing, invalid, still has a placeholder description, lost
    /// a required section or lists outdated commands.
    /// </summary>
    Target ValidateAgentSkills => d => d
        .Description("Checks that the agent skills required by #@ markers exist and list the current make targets")
        .Executes(() => {
            var actions = AgentSkillTasks.ReadMakeTargets(RootDirectory, AgentSkillsPrefix);
            var errors = AgentSkillTasks.Validate(SkillDirectories(), actions);
            foreach (var error in errors) Log.Error("{Error}", error);
            if (errors.Count > 0) Assert.Fail($"{errors.Count} agent skill problem(s)");
            Log.Information("The agent skills cover all {Count} marked make targets",
                actions.Count(action => action.Skill is not null));
        });

    /// <summary>
    /// Creates the missing skills from a template and rewrites the commands block of every required skill, keeping
    /// the rest of each file.
    /// </summary>
    Target SyncAgentSkills => d => d
        .Description("Creates missing agent skills and refreshes their generated commands from the make targets")
        .Executes(() => {
            var written = AgentSkillTasks.Sync(SkillDirectories(),
                AgentSkillTasks.ReadMakeTargets(RootDirectory, AgentSkillsPrefix),
                message => Log.Information("{Message}", message));
            Log.Information("{Count} skill file(s) written", written.Count);
        });

    private string[] SkillDirectories() =>
        [.. AgentSkillsDirectories.Select(directory => Path.GetFullPath(directory, RootDirectory))];
}
