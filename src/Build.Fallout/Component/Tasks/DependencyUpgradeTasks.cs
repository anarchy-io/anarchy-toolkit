using Anarchy.Toolkit.Build.Cli.Core.Service;
using NuGet.Versioning;
using Serilog;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks;

/// <summary>
/// Lists outdated NuGet packages and upgrades them with <c>dotnet-outdated</c>, including centrally pinned
/// transitive packages in <c>Directory.Packages.props</c>.
/// </summary>
internal static class DependencyUpgradeTasks
{
    /// <summary>
    /// Lists outdated packages, then (after confirmation, unless <paramref name="installTool"/> or
    /// <paramref name="upgrade"/> grants it) installs <c>dotnet-outdated-tool</c> and upgrades the dependencies.
    /// Without <paramref name="interactive"/> every confirmation is answered no. <paramref name="versionLock"/>
    /// is <c>Major</c>, <c>Minor</c> or <c>None</c> (case-insensitive).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="versionLock"/> is not a known lock.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a required command fails or the tool still cannot run after installing it.
    /// </exception>
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
        // dotnet-outdated only upgrades direct references. Centrally pinned transitive
        // packages must be upgraded too, before their dependents are changed.
        var props = Path.Combine(root, "Directory.Packages.props");
        if (File.Exists(props))
        {
            var packages = XDocument.Load(props).Descendants().Where(x => x.Name.LocalName == "PackageVersion")
                .Select(x => (string?)x.Attribute("Include")).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var report = Path.Combine(Path.GetTempPath(), $"dependency-upgrade-{Guid.NewGuid():N}.json");
            try
            {
                if (packages.Length != 0)
                {
                    var args = new List<string> { solution, "--transitive", "--transitive-depth", "20", "--version-lock", normalized, "--output", report };
                    foreach (var package in packages) { args.Add("--include"); args.Add(package!); }
                    RunRequired(tool, args, env);
                    if (File.Exists(report)) UpdateCentralVersions(props, report);
                }
            }
            finally { if (File.Exists(report)) File.Delete(report); }
        }
        RunRequired(tool, [solution, "--upgrade", "--no-restore", "--version-lock", normalized], env);
        RunRequired(dotnetExecutable, ["restore", solution]);
    }

    /// <summary>
    /// Applies the upgrades in a <c>dotnet-outdated</c> JSON <paramref name="report"/> to the unconditional
    /// <c>PackageVersion</c> items of <paramref name="props"/>, considering only projects governed by that file and
    /// preserving its formatting and encoding.
    /// </summary>
    internal static void UpdateCentralVersions(string props, string report)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(report));
        var updates = new Dictionary<string, List<(NuGetVersion Current, NuGetVersion Latest)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in json.RootElement.GetProperty("Projects").EnumerateArray())
        {
            // A nested central file takes precedence over the root file.
            var directory = Path.GetDirectoryName(Path.GetFullPath(project.GetProperty("FilePath").GetString()!));
            var root = Path.GetDirectoryName(Path.GetFullPath(props))!;
            while (directory != null && !string.Equals(directory, root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                if (File.Exists(Path.Combine(directory, "Directory.Packages.props"))) break;
                directory = Directory.GetParent(directory)?.FullName;
            }
            if (directory == null || !string.Equals(directory, root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) continue;
            foreach (var framework in project.GetProperty("TargetFrameworks").EnumerateArray())
                foreach (var dependency in framework.GetProperty("Dependencies").EnumerateArray())
                {
                    var name = dependency.GetProperty("Name").GetString();
                    if (name == null || !NuGetVersion.TryParse(dependency.GetProperty("ResolvedVersion").GetString(), out var current)
                                     || !NuGetVersion.TryParse(dependency.GetProperty("LatestVersion").GetString(), out var latest) || latest <= current) continue;
                    if (!updates.TryGetValue(name, out var versions)) updates[name] = versions = [];
                    versions.Add((current, latest));
                }
        }

        var document = XDocument.Load(props, LoadOptions.PreserveWhitespace);
        var changed = false;
        foreach (var item in document.Descendants().Where(x => x.Name.LocalName == "PackageVersion"))
        {
            var name = (string?)item.Attribute("Include");
            var version = item.Attribute("Version");
            if (name == null || version == null || item.AncestorsAndSelf().Any(x => x.Attribute("Condition") != null)
                || !NuGetVersion.TryParse(version.Value, out var current) || !updates.TryGetValue(name, out var versions)) continue;
            var latest = versions.Where(x => x.Current == current).Select(x => x.Latest).OrderByDescending(x => x).FirstOrDefault();
            if (latest == null) continue;
            Log.Information("Updating central package {Package} from {Current} to {Latest}", name, current, latest);
            version.Value = latest.ToNormalizedString();
            changed = true;
        }

        if (changed)
        {
            var original = File.ReadAllBytes(props);
            using var input = new StreamReader(new MemoryStream(original), detectEncodingFromByteOrderMarks: true);
            input.ReadToEnd();
            var encoding = input.CurrentEncoding;
            if (encoding.CodePage == Encoding.UTF8.CodePage)
                encoding = new UTF8Encoding(original.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
            using var output = new MemoryStream();
            using (var writer = XmlWriter.Create(output, new XmlWriterSettings {
                Encoding = encoding,
                OmitXmlDeclaration = document.Declaration == null,
                Indent = false,
                NewLineHandling = NewLineHandling.None
            })) document.Save(writer);
            File.WriteAllBytes(props, output.ToArray());
        }
    }
}
