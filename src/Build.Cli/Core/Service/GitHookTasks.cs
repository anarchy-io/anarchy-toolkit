using Serilog;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// Creates, removes and shows the Git hooks that run the Makefile's lint and test targets.
/// </summary>
public static class GitHookTasks
{
    private static string DirectoryPath(string root)
    {
        var path = LocalProcessTasks.Require(root, "git", "rev-parse", "--git-path", "hooks").TrimEnd('\r', '\n');
        return Path.GetFullPath(path, root);
    }

    private static string HookPath(string root, string type)
    {
        if (!Regex.IsMatch(type, "^[a-z0-9][a-z0-9-]*$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Invalid Git hook type.", nameof(type));
        return Path.Combine(DirectoryPath(root), type);
    }

    /// <summary>
    /// The hook script: a bash script running the Makefile targets that are not skipped, except during a rebase.
    /// An empty string when every group of targets is skipped.
    /// </summary>
    /// <param name="skipLint">Skips <c>lint_fix</c>, which re-stages the files it fixes.</param>
    /// <param name="skipStatic">Skips <c>lint</c> and <c>test_unit</c>.</param>
    /// <param name="skipRuntime">Skips <c>test_integration</c> and <c>test_end_to_end</c>.</param>
    public static string Content(bool skipLint, bool skipStatic, bool skipRuntime)
    {
        var tasks = new List<string>();
        if (!skipLint) tasks.Add("lint_fix AUTO_STAGE=true");
        if (!skipStatic) tasks.Add("lint test_unit");
        if (!skipRuntime) tasks.Add("test_integration test_end_to_end");
        if (tasks.Count == 0) return "";
        return "#!/usr/bin/env bash\nset -e\n\n" +
            "PROJECT_ROOT=\"$(git rev-parse --show-toplevel)\"\n" +
            "if [ -d \"$(git rev-parse --git-path rebase-merge)\" ] || [ -d \"$(git rev-parse --git-path rebase-apply)\" ]; then\n" +
            "    echo 'Skipping Git hook during rebase.'\n    exit 0\nfi\n" +
            $"make -C \"$PROJECT_ROOT\" -f Makefile {string.Join(" ", tasks)}\n";
    }

    /// <summary>
    /// Writes an executable hook of <paramref name="type"/> (e.g. <c>pre-commit</c>) into the repository's hooks
    /// directory; with <paramref name="dryRun"/>, only logs it. Nothing is written when every task is skipped.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not a valid hook name.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the hook already exists and <paramref name="force"/> is not set, or when <c>git</c> fails.
    /// </exception>
    public static void Enable(string root, string type, bool dryRun, bool force, bool skipLint, bool skipStatic, bool skipRuntime)
    {
        var path = HookPath(root, type);
        var content = Content(skipLint, skipStatic, skipRuntime);
        if (content.Length == 0) { Log.Warning("All tasks skipped; no hook created."); return; }
        if (dryRun) { Log.Information("Hook {Path}:\n{Content}", path, content); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!force && File.Exists(path)) throw new InvalidOperationException($"Hook already exists: {path}. Use --hook-force to replace it.");
        File.WriteAllText(path, content);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        Log.Information("Created hook {Path}", path);
    }

    /// <summary>
    /// Deletes the hook of <paramref name="type"/>, if any.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not a valid hook name.</exception>
    public static void Disable(string root, string type)
    {
        var path = HookPath(root, type);
        if (File.Exists(path)) { File.Delete(path); Log.Information("Removed hook {Path}", path); }
        else Log.Information("No hook found at {Path}", path);
    }

    /// <summary>
    /// The content of the hook of <paramref name="type"/> or, without a type, of every active hook: executable files
    /// other than Git's <c>.sample</c> ones.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not a valid hook name.</exception>
    public static string Show(string root, string? type)
    {
        if (!string.IsNullOrEmpty(type))
        {
            var path = HookPath(root, type);
            return File.Exists(path) ? $"{type}:\n{File.ReadAllText(path)}" : $"No hook found at {path}";
        }
        var directory = DirectoryPath(root);
        if (!Directory.Exists(directory)) return "No hooks found.";
        var files = Directory.EnumerateFiles(directory).Where(path => !path.EndsWith(".sample", StringComparison.Ordinal))
            .Where(path => OperatingSystem.IsWindows() || (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0)
            .Order(StringComparer.Ordinal).ToArray();
        return files.Length == 0 ? "No active hooks found." : string.Join("\n", files.Select(path => $"{Path.GetFileName(path)}:\n{File.ReadAllText(path)}"));
    }
}
