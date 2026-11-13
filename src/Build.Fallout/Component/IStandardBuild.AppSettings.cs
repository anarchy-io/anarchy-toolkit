using Anarchy.Toolkit.Build.Cli.Core.Service;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Serilog;
using System.Text.Json;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    public partial interface IStandardBuild
    {
        /// <summary>The tracked template listing what a developer has to override locally.</summary>
        const string AppSettingsTemplateFileName = "appsettings.json.template";

        /// <summary>The untracked file generated next to each template.</summary>
        const string DevelopmentAppSettingsFileName = "appsettings.Development.json";

        [Parameter("Asks again for every appsettings.json.template key, offering the current value as the default")]
        bool ResetAppSettings => TryGetValue<bool?>(() => ResetAppSettings) ?? false;

        /// <summary>
        /// The <c>appsettings.json.template</c> files <see cref="GenerateAppSettings"/> works on. By default, every
        /// one under <see cref="FalloutBuild.RootDirectory"/>, skipping <c>bin</c>, <c>obj</c> and <c>node_modules</c>.
        /// </summary>
        IReadOnlyCollection<AbsolutePath> AppSettingsTemplates => RootDirectory
            .GlobFiles($"**/{AppSettingsTemplateFileName}")
            .Where(path => !IsInIgnoredDirectory(RootDirectory.GetRelativePathTo(path)))
            .OrderBy(path => path.ToString(), StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// <para>
        /// Generates the untracked <c>appsettings.Development.json</c> next to every
        /// <c>appsettings.json.template</c> (see <see cref="AppSettingsTemplates"/>).
        /// </para>
        ///
        /// <para>
        /// A template holds only the keys a developer has to override from the shared <c>appsettings.json</c>; each
        /// value is a string explaining what is expected and, optionally, where to find it. Every key that is not
        /// yet set in <c>appsettings.Development.json</c> is prompted for, showing that explanation. Values already
        /// set, and keys that are not in the template, are kept, so the target can be re-run whenever the template
        /// gains a key. Pass <c>--reset-app-settings</c> to be asked for every key again.
        /// </para>
        ///
        /// <para>
        /// Template values that are not strings (numbers, booleans, arrays) are fixed defaults, copied as-is when
        /// missing. Keys whose name suggests a secret (password, secret, token, API key) are read with masked input.
        /// Without an interactive console (CI, redirected input) nothing is prompted: the missing keys are only
        /// reported.
        /// </para>
        /// </summary>
        Target GenerateAppSettings => d => d
            .Description("Generates the untracked appsettings.Development.json files from their appsettings.json.template")
            .TriggeredBy(SetupLocal)
            .Executes(() => {
                var templates = AppSettingsTemplates;

                if (templates.Count == 0)
                {
                    Log.Warning("No {Template} files found. Skipping", AppSettingsTemplateFileName);
                    return;
                }

                var interactive = !IsServerBuild && !IsCiBuild && !Console.IsInputRedirected;

                if (!interactive)
                {
                    Log.Warning("No interactive console: missing settings will only be reported");
                }

                foreach (var template in templates)
                {
                    GenerateAppSettingsFrom(template, interactive ? PromptForAppSetting : null);
                }
            });

        internal void GenerateAppSettingsFrom(AbsolutePath templateFile, AppSettingsMerger.Prompt? prompt)
        {
            var outputFile = templateFile.Parent / DevelopmentAppSettingsFileName;
            var relativeOutput = RootDirectory.GetRelativePathTo(outputFile).ToString().Replace('\\', '/');

            Log.Information("Generating {Output}", relativeOutput);

            var template = AppSettingsMerger.ParseJsonObject(templateFile.ReadAllText())
                        ?? throw new Exception($"'{RootDirectory.GetRelativePathTo(templateFile)}' must hold a JSON object.");
            var existing = outputFile.FileExists() ? AppSettingsMerger.ParseJsonObject(outputFile.ReadAllText()) : null;

            var result = AppSettingsMerger.Merge(template, existing, ResetAppSettings, prompt);

            if (result.Settings.Count > 0 || existing is not null)
            {
                outputFile.WriteAllText(result.Settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
                Log.Information("Wrote {Output}", relativeOutput);
            }

            if (result.Missing.Count > 0)
            {
                Log.Warning("{Output} is still missing: {Keys}. Re-run this target to fill them in",
                    relativeOutput, result.Missing.Join(", "));
            }
        }

        private static string? PromptForAppSetting(string key, string hint, string? currentValue)
        {
            var sensitive = AppSettingsMerger.IsSensitive(key);

            Console.WriteLine();
            Console.WriteLine($"  {key}");
            Console.WriteLine($"    {hint}");

            if (currentValue is not null)
            {
                Console.WriteLine($"    Current: {(sensitive ? new string('*', currentValue.Length) : currentValue)} (Enter keeps it)");
            }

            Console.Write("  > ");

            return sensitive ? ConsoleUtility.ReadSecret() : Console.ReadLine();
        }

        private static bool IsInIgnoredDirectory(RelativePath path) => AppSettingsMerger.IsInIgnoredDirectory(path.ToString());
    }
}
