using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// Reads and updates Fallout's <c>.fallout/parameters.json</c>; parameter names match case-insensitively.
/// </summary>
public static class FalloutParameters
{
    /// <summary>
    /// The path of the parameters file under a solution root.
    /// </summary>
    public static string PathOf(string root) => Path.Combine(root, ".fallout", "parameters.json");

    /// <summary>
    /// The parameters, or an empty object if the file does not exist.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the file does not contain a JSON object.</exception>
    public static JsonObject Read(string root)
    {
        var path = PathOf(root);
        return File.Exists(path)
            ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject
              ?? throw new InvalidOperationException($"{path} must contain a JSON object.")
            : new JsonObject();
    }

    /// <summary>
    /// Adds parameters, creating the file if needed; existing ones are never replaced.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a parameter is already defined (nothing is written then), or the file does not contain a JSON object.
    /// </exception>
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

    /// <summary>
    /// A string parameter, or null if it is missing or not a string.
    /// </summary>
    public static string? GetString(JsonObject parameters, string key) =>
        Find(parameters, key) is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>
    /// A boolean parameter, also accepted as a <c>"true"</c>/<c>"false"</c> string; null if missing or invalid.
    /// </summary>
    public static bool? GetBool(JsonObject parameters, string key) => Find(parameters, key) switch {
        JsonValue value when value.TryGetValue<bool>(out var flag) => flag,
        JsonValue value when value.TryGetValue<string>(out var text) && bool.TryParse(text, out var flag) => flag,
        _ => null
    };

    /// <summary>
    /// A string array parameter; Fallout also accepts a single space-separated string. Null if missing.
    /// </summary>
    public static IReadOnlyList<string>? GetStrings(JsonObject parameters, string key) => Find(parameters, key) switch {
        JsonArray array => array.Select(item => item?.GetValue<string>()).OfType<string>().ToArray(),
        JsonValue value when value.TryGetValue<string>(out var text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries),
        _ => null
    };

    private static JsonNode? Find(JsonObject parameters, string key) =>
        parameters.FirstOrDefault(property => string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
}
