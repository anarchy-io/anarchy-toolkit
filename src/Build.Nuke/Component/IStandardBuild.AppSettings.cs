using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Serilog;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Nuke.Component
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
        /// one under <see cref="NukeBuild.RootDirectory"/>, skipping <c>bin</c>, <c>obj</c> and <c>node_modules</c>.
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

        internal void GenerateAppSettingsFrom(AbsolutePath templateFile, AppSettingPrompt? prompt)
        {
            var outputFile = templateFile.Parent / DevelopmentAppSettingsFileName;
            var relativeOutput = RootDirectory.GetRelativePathTo(outputFile).ToString().Replace('\\', '/');

            Log.Information("Generating {Output}", relativeOutput);

            var template = ParseJsonObject(templateFile)
                        ?? throw new Exception($"'{RootDirectory.GetRelativePathTo(templateFile)}' must hold a JSON object.");
            var existing = outputFile.FileExists() ? ParseJsonObject(outputFile) : null;

            var result = MergeAppSettings(template, existing, ResetAppSettings, prompt);

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

        /// <summary>Asks for one setting; <c>null</c> or blank means "keep the current value, if any".</summary>
        internal delegate string? AppSettingPrompt(string key, string hint, string? currentValue);

        internal sealed record AppSettingsMergeResult(JsonObject Settings, IReadOnlyList<string> Missing);

        /// <summary>
        /// Fills <paramref name="existing"/> (or a new object) with every key of <paramref name="template"/> that is
        /// not set yet, asking <paramref name="prompt"/> for string ones. A value equal to its template hint counts as
        /// not set. With <paramref name="reset"/>, set keys are asked for again too, offering their value as default.
        /// </summary>
        internal static AppSettingsMergeResult MergeAppSettings(
            JsonObject template, JsonObject? existing, bool reset, AppSettingPrompt? prompt)
        {
            var settings = existing?.DeepClone().AsObject() ?? new JsonObject();
            var missing = new List<string>();

            MergeSection(template, settings, prefix: "", reset, prompt, missing);

            return new AppSettingsMergeResult(settings, missing);
        }

        private static void MergeSection(
            JsonObject template, JsonObject target, string prefix, bool reset, AppSettingPrompt? prompt, List<string> missing)
        {
            foreach (var (name, templateNode) in template)
            {
                var key = prefix.Length == 0 ? name : $"{prefix}:{name}";

                if (templateNode is JsonObject templateSection)
                {
                    var createdSection = target[name] is not JsonObject;
                    if (createdSection)
                    {
                        target[name] = new JsonObject();
                    }

                    var section = target[name]!.AsObject();
                    MergeSection(templateSection, section, key, reset, prompt, missing);

                    if (createdSection && section.Count == 0)
                    {
                        target.Remove(name);
                    }

                    continue;
                }

                var hint = templateNode is JsonValue value && value.GetValueKind() == JsonValueKind.String
                    ? value.GetValue<string>()
                    : null;
                var current = target.TryGetPropertyValue(name, out var currentNode) ? currentNode : null;
                var isSet = target.ContainsKey(name) && !(hint is not null && IsHint(current, hint));

                if (hint is null)
                {
                    // Not an explanation but a fixed default: copy it unless the developer already set one.
                    if (!isSet)
                    {
                        target[name] = templateNode?.DeepClone();
                    }

                    continue;
                }

                if (isSet && !reset)
                {
                    continue;
                }

                var currentValue = isSet ? current?.ToString() : null;
                var answer = prompt?.Invoke(key, hint, currentValue);

                if (!string.IsNullOrWhiteSpace(answer))
                {
                    target[name] = answer.Trim();
                }
                else if (!isSet)
                {
                    // Never leave the explanation behind as a value the app would happily use.
                    target.Remove(name);
                    missing.Add(key);
                }
            }
        }

        private static bool IsHint(JsonNode? node, string hint) =>
            node is JsonValue value
         && value.GetValueKind() == JsonValueKind.String
         && value.GetValue<string>() == hint;

        /// <summary>Whether a setting key looks like it holds a secret, to read it with masked input.</summary>
        internal static bool IsSensitiveAppSetting(string key)
        {
            var name = key[(key.LastIndexOf(':') + 1)..];

            return new[] { "password", "secret", "token", "apikey", "api_key", "privatekey" }
                .Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase))
                && !name.EndsWith("Name", StringComparison.Ordinal);
        }

        private static string? PromptForAppSetting(string key, string hint, string? currentValue)
        {
            var sensitive = IsSensitiveAppSetting(key);

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

        private static JsonObject? ParseJsonObject(AbsolutePath file) =>
            JsonNode.Parse(
                file.ReadAllText(),
                documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }
            ) as JsonObject;

        private static bool IsInIgnoredDirectory(RelativePath path) =>
            path.ToString()
                .Split('/', '\\')
                .Any(segment => segment is "bin" or "obj" or "node_modules" || segment.StartsWith('.'));
    }
}
