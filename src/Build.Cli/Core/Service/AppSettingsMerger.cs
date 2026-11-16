using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// Merges an <c>appsettings.json.template</c> into an <c>appsettings.Development.json</c>: see the
/// <c>GenerateAppSettings</c> Fallout target.
/// </summary>
/// <remarks>
/// The template's string values are human-readable hints rather than values; any other value (number, boolean,
/// array, ...) is a fixed default. Nested objects are merged recursively and their keys reported in configuration
/// notation (<c>Section:Key</c>).
/// </remarks>
/// <example>
/// <code language="csharp">
/// var template = AppSettingsMerger.ParseJsonObject(File.ReadAllText("appsettings.json.template"))!;
/// var result = AppSettingsMerger.Merge(template, existing: null, reset: false,
///     prompt: (key, hint, current) =&gt; Console.ReadLine());
/// File.WriteAllText("appsettings.Development.json", result.Settings.ToJsonString());
/// </code>
/// </example>
public static class AppSettingsMerger
{
    /// <summary>
    /// Asks for one setting; <c>null</c> or blank means "keep the current value, if any".
    /// </summary>
    /// <param name="key">The setting key, in configuration notation (e.g. <c>Database:Password</c>).</param>
    /// <param name="hint">The template's hint for the setting.</param>
    /// <param name="currentValue">The value already set, or <see langword="null"/> if none.</param>
    /// <returns>The answer, or <see langword="null"/>/blank to keep the current value.</returns>
    public delegate string? Prompt(string key, string hint, string? currentValue);

    /// <summary>
    /// The merged settings, and the keys of the settings left unset (no value and no answer).
    /// </summary>
    /// <param name="Settings">The merged settings.</param>
    /// <param name="Missing">The keys left unset, in configuration notation.</param>
    public sealed record MergeResult(JsonObject Settings, IReadOnlyList<string> Missing);

    /// <summary>
    /// Fills <paramref name="existing"/> (or a new object) with every key of <paramref name="template"/> that is
    /// not set yet: string values are hints, so <paramref name="prompt"/> is asked for them; other values are fixed
    /// defaults and copied as-is. A value equal to its template hint counts as not set. With <paramref name="reset"/>,
    /// set string keys are asked for again too, offering their value as default. <paramref name="existing"/> is not
    /// modified.
    /// </summary>
    /// <param name="template">The parsed template.</param>
    /// <param name="existing">The current settings, or <see langword="null"/> to start from an empty object.</param>
    /// <param name="reset">Whether to ask again for string settings that are already set.</param>
    /// <param name="prompt">Asks for each setting; <see langword="null"/> leaves every unset hint key missing.</param>
    /// <returns>The merged settings and the keys left unset.</returns>
    public static MergeResult Merge(
        JsonObject template, JsonObject? existing, bool reset, Prompt? prompt)
    {
        var settings = existing?.DeepClone().AsObject() ?? new JsonObject();
        var missing = new List<string>();

        MergeSection(template, settings, prefix: "", reset, prompt, missing);

        return new MergeResult(settings, missing);
    }

    private static void MergeSection(
        JsonObject template, JsonObject target, string prefix, bool reset, Prompt? prompt, List<string> missing)
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

    /// <summary>
    /// Whether a setting key looks like it holds a secret, to read it with masked input. Keys ending in <c>Name</c>
    /// (e.g. <c>ClientSecretName</c>) only name a secret and are not sensitive.
    /// </summary>
    /// <param name="key">The setting key; only its last <c>:</c>-separated segment is considered.</param>
    /// <returns><see langword="true"/> if the key mentions a password, secret, token, API key or private key.</returns>
    public static bool IsSensitive(string key)
    {
        var name = key[(key.LastIndexOf(':') + 1)..];

        return new[] { "password", "secret", "token", "apikey", "api_key", "privatekey" }
            .Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase))
            && !name.EndsWith("Name", StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses a settings file's content, allowing comments and trailing commas; <c>null</c> when it is not an object.
    /// </summary>
    /// <param name="json">The file content.</param>
    /// <returns>The root object, or <see langword="null"/>.</returns>
    /// <exception cref="System.Text.Json.JsonException">Thrown by the parser when <paramref name="json"/> is not valid JSON.</exception>
    public static JsonObject? ParseJsonObject(string json) =>
        JsonNode.Parse(
            json,
            documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }
        ) as JsonObject;

    /// <summary>
    /// Whether a relative path goes through <c>bin</c>, <c>obj</c>, <c>node_modules</c> or a hidden folder.
    /// </summary>
    /// <param name="relativePath">The path, with <c>/</c> or <c>\</c> separators.</param>
    /// <returns><see langword="true"/> if any segment is ignored.</returns>
    public static bool IsInIgnoredDirectory(string relativePath) =>
        relativePath
            .Split('/', '\\')
            .Any(segment => segment is "bin" or "obj" or "node_modules" || segment.StartsWith('.'));
}
