using Anarchy.Toolkit.Automation.Core.Service;
using Fallout.Common;
using Serilog;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Optional local Git hook management. Hook execution uses the repository's Makefile.
/// </summary>
/// <remarks>
/// Hooks are local to the clone and never committed. The work is done by
/// <see cref="Anarchy.Toolkit.Automation.Core.Service.GitHookTasks"/>.
/// </remarks>
public interface IGitHooksBuild : IAutomationPreferencesBuild
{
    /// <summary>
    /// The Git hook to act on (<c>--hook-type</c>), e.g. <c>pre-commit</c> or <c>pre-push</c>. Defaults to
    /// <c>pre-commit</c> for <see cref="EnableGitHook"/> and <see cref="DisableGitHook"/>, and to all hooks for
    /// <see cref="ShowGitHooks"/>.
    /// </summary>
    [Parameter("Git hook type (default: pre-commit for enable/disable, all for show)")]
    string? HookType => TryGetValue(() => HookType);

    /// <summary>
    /// Whether <see cref="EnableGitHook"/> only previews the hook instead of writing it (<c>--hook-dry-run</c>).
    /// </summary>
    [Parameter("Preview hook without writing")]
    bool HookDryRun => TryGetValue<bool?>(() => HookDryRun) ?? false;

    /// <summary>
    /// Whether <see cref="EnableGitHook"/> may replace an existing hook (<c>--hook-force</c>).
    /// </summary>
    [Parameter("Allow replacing an existing hook")]
    bool HookForce => TryGetValue<bool?>(() => HookForce) ?? false;

    /// <summary>
    /// Whether the generated hook skips lint fixing (<c>--hook-skip-lint-fix</c>).
    /// </summary>
    [Parameter("Omit lint fixing from the hook")]
    bool HookSkipLintFix => TryGetValue<bool?>(() => HookSkipLintFix) ?? Preference<bool?>("gitHooks.skipLintFix") ?? false;

    /// <summary>
    /// Whether the generated hook skips lint and unit tests (<c>--hook-skip-test-static</c>).
    /// </summary>
    [Parameter("Omit lint and unit tests from the hook")]
    bool HookSkipTestStatic => TryGetValue<bool?>(() => HookSkipTestStatic) ?? Preference<bool?>("gitHooks.skipTestStatic") ?? false;

    /// <summary>
    /// Whether the generated hook skips integration and end-to-end tests (<c>--hook-skip-test-runtime</c>).
    /// </summary>
    [Parameter("Omit integration and end-to-end tests from the hook")]
    bool HookSkipTestRuntime => TryGetValue<bool?>(() => HookSkipTestRuntime) ?? Preference<bool?>("gitHooks.skipTestRuntime") ?? false;

    /// <summary>
    /// Writes the <see cref="HookType"/> hook into the local repository, honouring the <c>Hook*</c> options.
    /// </summary>
    Target EnableGitHook => d => d.Description("Create a local Git hook")
        .Executes(() => GitHookTasks.Enable(RootDirectory, HookType ?? "pre-commit", HookDryRun, HookForce,
            HookSkipLintFix, HookSkipTestStatic, HookSkipTestRuntime));
    /// <summary>
    /// Removes the <see cref="HookType"/> hook from the local repository.
    /// </summary>
    Target DisableGitHook => d => d.Description("Remove a local Git hook")
        .Executes(() => GitHookTasks.Disable(RootDirectory, HookType ?? "pre-commit"));
    /// <summary>
    /// Logs the <see cref="HookType"/> hook, or every hook when not set.
    /// </summary>
    Target ShowGitHooks => d => d.Description("Show local Git hooks")
        .Executes(() => Log.Information("{Hooks}", GitHookTasks.Show(RootDirectory, HookType)));
}
