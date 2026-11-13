using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>Reads and updates Fallout's <c>.fallout/parameters.json</c>.</summary>
public static class FalloutParameters
{
    public static string PathOf(string root) => Path.Combine(root, ".fallout", "parameters.json");

    /// <summary>The parameters, or an empty object if the file does not exist.</summary>
    public static JsonObject Read(string root)
    {
        var path = PathOf(root);
        return File.Exists(path)
            ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject
              ?? throw new InvalidOperationException($"{path} must contain a JSON object.")
            : new JsonObject();
    }

    /// <summary>Adds parameters; existing ones are never replaced.</summary>
    public static void Add(string root, IReadOnlyDictionary<string, object> values)
    {
        var parameters = Read(root);
        foreach (var (key, value) in values)
        {
            if (parameters.Any(property => string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Fallout parameters.json already defines {key}; it will not be overwritten.");
            parameters[key] = JsonSerializer.SerializeToNode(value);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PathOf(root))!);
        File.WriteAllText(PathOf(root), parameters.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    public static string? GetString(JsonObject parameters, string key) =>
        Find(parameters, key) is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static bool? GetBool(JsonObject parameters, string key) => Find(parameters, key) switch {
        JsonValue value when value.TryGetValue<bool>(out var flag) => flag,
        JsonValue value when value.TryGetValue<string>(out var text) && bool.TryParse(text, out var flag) => flag,
        _ => null
    };

    /// <summary>A string array parameter; Fallout also accepts a single space-separated string.</summary>
    public static IReadOnlyList<string>? GetStrings(JsonObject parameters, string key) => Find(parameters, key) switch {
        JsonArray array => array.Select(item => item?.GetValue<string>()).OfType<string>().ToArray(),
        JsonValue value when value.TryGetValue<string>(out var text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries),
        _ => null
    };

    private static JsonNode? Find(JsonObject parameters, string key) =>
        parameters.FirstOrDefault(property => string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
}
