using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Build.Cli.Core.Validation;

/// <summary>Valid solution and application names.</summary>
public static partial class NameRules
{
    public const string SolutionNameHelp =
        "Solution names must start with a letter or digit and contain only letters, digits, dots, underscores or hyphens.";

    public const string ApplicationNameHelp =
        "Application names must start with a letter and contain only letters and digits (e.g. Orders).";

    public static bool IsValidSolutionName(string? name) => name is not null && SolutionNameRegex().IsMatch(name);

    public static bool IsValidApplicationName(string? name) => name is not null && ApplicationNameRegex().IsMatch(name);

    /// <summary>Why the layers, innermost first, cannot be used, or null if they can.</summary>
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
