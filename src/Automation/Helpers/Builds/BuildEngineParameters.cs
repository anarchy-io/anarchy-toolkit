using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Helpers.Builds;

/// <summary>
/// Reads Fallout's <c>.fallout/parameters.json</c>; parameter names match case-insensitively.
/// </summary>
/// <remarks>
/// <para>
/// The file belongs to the build engine: Fallout reads its parameter defaults from it (such as <c>Solution</c>, which
/// the scaffolder records there), regenerates <c>build.schema.json</c> next to it and keeps its <c>temp/</c> folder
/// there. Fallout hard-codes the <c>.fallout</c> name for all of them, so they cannot move.
/// </para>
/// <para>
/// Optional toolkit overrides, including layering, live in the solution-root <c>.anarchy-toolkit.json</c> instead.
/// </para>
/// </remarks>
public static class BuildEngineParameters
{
    /// <summary>
    /// The engine's directory name, relative to the solution root.
    /// </summary>
    public const string DirectoryName = ".fallout";

    /// <summary>
    /// The path of the parameters file under a solution root.
    /// </summary>
    /// <param name="root">The solution directory.</param>
    /// <returns>The path of <c>.fallout/parameters.json</c>.</returns>
    public static string PathOf(string root) => Path.Combine(root, DirectoryName, "parameters.json");

    /// <summary>
    /// The parameters, or an empty object if the file does not exist.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the file does not contain a JSON object.</exception>
    /// <param name="root">The solution directory.</param>
    /// <returns>The parameters.</returns>
    public static JsonObject Read(string root)
    {
        var path = PathOf(root);
        return File.Exists(path)
            ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject
              ?? throw new InvalidOperationException($"{path} must contain a JSON object.")
            : new JsonObject();
    }

    /// <summary>
    /// A string parameter, or null if it is missing or not a string.
    /// </summary>
    /// <param name="parameters">The parameters, as returned by <see cref="Read"/>.</param>
    /// <param name="key">The parameter name.</param>
    /// <returns>The value, or <see langword="null"/>.</returns>
    public static string? GetString(JsonObject parameters, string key) =>
        Find(parameters, key) is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static JsonNode? Find(JsonObject parameters, string key) =>
        parameters.FirstOrDefault(property => string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
}
