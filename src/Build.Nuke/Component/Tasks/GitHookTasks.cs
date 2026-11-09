using Serilog;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    internal static class GitHookTasks
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

        internal static string Content(bool skipLint, bool skipStatic, bool skipRuntime)
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

        internal static void Enable(string root, string type, bool dryRun, bool force, bool skipLint, bool skipStatic, bool skipRuntime)
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

        internal static void Disable(string root, string type)
        {
            var path = HookPath(root, type);
            if (File.Exists(path)) { File.Delete(path); Log.Information("Removed hook {Path}", path); }
            else Log.Information("No hook found at {Path}", path);
        }

        internal static string Show(string root, string? type)
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
}
