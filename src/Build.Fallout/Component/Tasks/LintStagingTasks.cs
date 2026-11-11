using Serilog;
using System.Security.Cryptography;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    internal static class LintStagingTasks
    {
        internal static void Run(string root, bool autoStage, Action lint)
        {
            if (!autoStage) { lint(); return; }
            static string[] Paths(string output) => output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            var staged = Paths(LocalProcessTasks.Require(root, "git", "diff", "--cached", "--name-only", "--diff-filter=ACM", "-z", "--"));
            var dirty = Paths(LocalProcessTasks.Require(root, "git", "diff", "--name-only", "-z", "--")).ToHashSet(StringComparer.Ordinal);
            var snapshots = new Dictionary<string, (string Index, byte[] Hash)>(StringComparer.Ordinal);
            foreach (var path in staged)
            {
                if (dirty.Contains(path)) { Log.Warning("Not auto-staging partially staged file {Path}", path); continue; }
                var fullPath = Path.Combine(root, path);
                if (!File.Exists(fullPath) || (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0) continue;
                snapshots[path] = (LocalProcessTasks.Require(root, "git", "ls-files", "--stage", "-z", "--", ":(literal)" + path), SHA256.HashData(File.ReadAllBytes(fullPath)));
            }
            lint();
            foreach (var (path, snapshot) in snapshots)
            {
                var fullPath = Path.Combine(root, path);
                if (!File.Exists(fullPath) || (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0 ||
                    snapshot.Index != LocalProcessTasks.Require(root, "git", "ls-files", "--stage", "-z", "--", ":(literal)" + path))
                {
                    Log.Warning("Index or file changed during lint; not auto-staging {Path}", path);
                    continue;
                }
                if (snapshot.Hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(fullPath)))) continue;
                LocalProcessTasks.Require(root, "git", "add", "--", ":(literal)" + path);
                Log.Information("Re-staged lint fixes in {Path}", path);
            }
        }
    }
}
