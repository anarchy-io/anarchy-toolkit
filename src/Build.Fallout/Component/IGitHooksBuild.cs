using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Fallout.Common;
using Serilog;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    /// <summary>Optional local Git hook management. Hook execution uses the repository's Makefile.</summary>
    public interface IGitHooksBuild : IFalloutBuild
    {
        [Parameter("Git hook type (default: pre-commit for enable/disable, all for show)")]
        string? HookType => TryGetValue(() => HookType);
        [Parameter("Preview hook without writing")]
        bool HookDryRun => TryGetValue<bool?>(() => HookDryRun) ?? false;
        [Parameter("Allow replacing an existing hook")]
        bool HookForce => TryGetValue<bool?>(() => HookForce) ?? false;
        [Parameter("Omit lint fixing from the hook")]
        bool HookSkipLintFix => TryGetValue<bool?>(() => HookSkipLintFix) ?? false;
        [Parameter("Omit lint and unit tests from the hook")]
        bool HookSkipTestStatic => TryGetValue<bool?>(() => HookSkipTestStatic) ?? false;
        [Parameter("Omit integration and end-to-end tests from the hook")]
        bool HookSkipTestRuntime => TryGetValue<bool?>(() => HookSkipTestRuntime) ?? false;

        Target EnableGitHook => d => d.Description("Create a local Git hook")
            .Executes(() => GitHookTasks.Enable(RootDirectory, HookType ?? "pre-commit", HookDryRun, HookForce,
                HookSkipLintFix, HookSkipTestStatic, HookSkipTestRuntime));
        Target DisableGitHook => d => d.Description("Remove a local Git hook")
            .Executes(() => GitHookTasks.Disable(RootDirectory, HookType ?? "pre-commit"));
        Target ShowGitHooks => d => d.Description("Show local Git hooks")
            .Executes(() => Log.Information("{Hooks}", GitHookTasks.Show(RootDirectory, HookType)));
    }
}
