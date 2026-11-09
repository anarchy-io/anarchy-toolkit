using Serilog;
using System.Runtime.InteropServices;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    internal static class DependencyUpgradeTasks
    {
        internal static void Run(string root, string solution, string versionLock, bool installTool, bool upgrade, bool interactive,
            string? dotnetExecutable = null, string? cliHome = null)
        {
            dotnetExecutable ??= "dotnet";
            var allowed = new[] { "Major", "Minor", "None" };
            var normalized = allowed.FirstOrDefault(x => x.Equals(versionLock, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("Dependency version lock must be Major, Minor or None.");
            void RunRequired(string executable, IEnumerable<string> args, IReadOnlyDictionary<string, string>? environment = null)
            {
                var result = LocalProcessTasks.Run(root, executable, args, environment, capture: false);
                if (result.ExitCode != 0) throw new InvalidOperationException($"{executable} exited with code {result.ExitCode}.");
            }
            bool Confirm(string question)
            {
                if (!interactive) return false;
                Console.Write($"{question} [y/N] ");
                return string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
            }

            RunRequired(dotnetExecutable, ["list", solution, "package", "--outdated"]);
            var home = cliHome ?? Environment.GetEnvironmentVariable("DOTNET_CLI_HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var tool = Path.Combine(home, ".dotnet", "tools", OperatingSystem.IsWindows() ? "dotnet-outdated.exe" : "dotnet-outdated");
            var runtimeRoot = Directory.GetParent(RuntimeEnvironment.GetRuntimeDirectory().TrimEnd(Path.DirectorySeparatorChar))?.Parent?.Parent?.FullName;
            var env = new Dictionary<string, string> {
                ["DOTNET_ROLL_FORWARD"] = Environment.GetEnvironmentVariable("DOTNET_ROLL_FORWARD") ?? "Major"
            };
            var configuredRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (string.IsNullOrEmpty(configuredRoot) || !File.Exists(Path.Combine(configuredRoot, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet")))
                if (runtimeRoot != null) env["DOTNET_ROOT"] = runtimeRoot;

            bool ToolWorks()
            {
                if (!File.Exists(tool)) return false;
                try { return LocalProcessTasks.Run(root, tool, ["--version"], env).ExitCode == 0; }
                catch (System.ComponentModel.Win32Exception) { return false; }
            }
            if (!ToolWorks())
            {
                if (!installTool && !Confirm("Install or reinstall the global dotnet-outdated-tool?"))
                { Log.Warning("dotnet-outdated-tool unavailable; upgrade skipped."); return; }
                // Uninstall only if installed; the tool may exist but fail due to runtime incompatibility.
                var installed = LocalProcessTasks.Require(root, dotnetExecutable, "tool", "list", "--global");
                if (installed.Split('\n').Any(line => line.TrimStart().StartsWith("dotnet-outdated-tool ", StringComparison.OrdinalIgnoreCase)))
                    RunRequired(dotnetExecutable, ["tool", "uninstall", "--global", "dotnet-outdated-tool"]);
                RunRequired(dotnetExecutable, ["tool", "install", "--global", "dotnet-outdated-tool"]);
                if (!ToolWorks()) throw new InvalidOperationException("dotnet-outdated-tool cannot run after installation.");
            }
            if (!upgrade && !Confirm($"Upgrade all outdated dependencies (version lock: {normalized})?"))
            { Log.Information("Dependency upgrade skipped."); return; }
            RunRequired(tool, [solution, "--upgrade", "--version-lock", normalized], env);
        }
    }
}
