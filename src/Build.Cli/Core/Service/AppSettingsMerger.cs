using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>
/// Merges an <c>appsettings.json.template</c> into an <c>appsettings.Development.json</c>: see the
/// <c>GenerateAppSettings</c> Fallout target.
/// </summary>
public static class AppSettingsMerger
{
    /// <summary>Asks for one setting; <c>null</c> or blank means "keep the current value, if any".</summary>
    public delegate string? Prompt(string key, string hint, string? currentValue);

    public sealed record MergeResult(JsonObject Settings, IReadOnlyList<string> Missing);

    /// <summary>
    /// Fills <paramref name="existing"/> (or a new object) with every key of <paramref name="template"/> that is
    /// not set yet, asking <paramref name="prompt"/> for string ones. A value equal to its template hint counts as
    /// not set. With <paramref name="reset"/>, set keys are asked for again too, offering their value as default.
    /// </summary>
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

    /// <summary>Whether a setting key looks like it holds a secret, to read it with masked input.</summary>
    public static bool IsSensitive(string key)
    {
        var name = key[(key.LastIndexOf(':') + 1)..];

        return new[] { "password", "secret", "token", "apikey", "api_key", "privatekey" }
            .Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase))
            && !name.EndsWith("Name", StringComparison.Ordinal);
    }

    /// <summary>Parses a settings file's content, allowing comments and trailing commas; <c>null</c> when it is not an object.</summary>
    public static JsonObject? ParseJsonObject(string json) =>
        JsonNode.Parse(
            json,
            documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }
        ) as JsonObject;

    /// <summary>Whether a relative path goes through <c>bin</c>, <c>obj</c>, <c>node_modules</c> or a hidden folder.</summary>
    public static bool IsInIgnoredDirectory(string relativePath) =>
        relativePath
            .Split('/', '\\')
            .Any(segment => segment is "bin" or "obj" or "node_modules" || segment.StartsWith('.'));
}
