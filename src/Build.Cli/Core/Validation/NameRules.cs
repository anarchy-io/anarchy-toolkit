using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Build.Cli.Core.Validation;

/// <summary>
/// Validation rules for solution, application and layer names, with their help messages.
/// </summary>
public static partial class NameRules
{
    /// <summary>
    /// Explains <see cref="IsValidSolutionName"/> to the user.
    /// </summary>
    public const string SolutionNameHelp =
        "Solution names must start with a letter or digit and contain only letters, digits, dots, underscores or hyphens.";

    /// <summary>
    /// Explains <see cref="IsValidApplicationName"/> to the user.
    /// </summary>
    public const string ApplicationNameHelp =
        "Application names must start with a letter and contain only letters and digits (e.g. Orders).";

    /// <summary>
    /// Whether a name can be used for a solution file: a letter or digit followed by letters, digits, dots, underscores or hyphens.
    /// </summary>
    public static bool IsValidSolutionName(string? name) => name is not null && SolutionNameRegex().IsMatch(name);

    /// <summary>
    /// Whether a name can be used for an application or layer: a letter followed by letters and digits.
    /// </summary>
    public static bool IsValidApplicationName(string? name) => name is not null && ApplicationNameRegex().IsMatch(name);

    /// <summary>
    /// Why the layers, innermost first, cannot be used, or null if they can: each must be a valid name, not
    /// <c>Testing</c> or <c>Library</c>, and listed once (ignoring case).
    /// </summary>
    public static string? ValidateLayers(IReadOnlyCollection<string> layers)
    {
        if (layers.Count == 0) return "Enter at least one layer.";
        if (layers.FirstOrDefault(layer => !IsValidApplicationName(layer)) is { } invalid)
            return $"'{invalid}': layer names must start with a letter and contain only letters and digits.";
        if (layers.FirstOrDefault(layer => layer is "Testing" or "Library") is { } reserved)
            return $"'{reserved}' is reserved and cannot be a layer.";
        return layers.GroupBy(layer => layer, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1) is { } duplicate
            ? $"Layer {duplicate.Key} is listed more than once."
            : null;
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex SolutionNameRegex();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$")]
    private static partial Regex ApplicationNameRegex();
}
