using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Resolves <see cref="ScaffoldOptions.Items"/> and <see cref="ScaffoldOptions.Exclude"/> against the available items.
/// </summary>
/// <remarks>
/// The agent skills (<c>.agents/skills</c>) are opt-in: only an explicit <c>skills</c> selector includes them.
/// </remarks>
internal sealed class ScaffoldSelection
{
    private static readonly string[] GeneratedItems = ["solution", "core", "tests", "samples", "docs", "Directory.Build.props"];
    private const string Skills = "skills";
    private const string SkillsPath = ".agents/skills/";
    private readonly string[] _includes;
    private readonly string[] _excludes;

    /// <exception cref="ArgumentException">Thrown when a selector matches no available item.</exception>
    public ScaffoldSelection(IEnumerable<string> resourcePaths, ScaffoldOptions options)
    {
        var available = resourcePaths.Concat(GeneratedItems).ToArray();
        _includes = Parse(options.Items, defaultAll: true);
        _excludes = Parse(options.Exclude, defaultAll: false);
        foreach (var selector in _includes.Concat(_excludes).Where(selector => selector is not ("all" or "none")))
        {
            if (!available.Any(item => Matches(item, selector)))
                throw new ArgumentException($"Unknown boilerplate item '{selector}'.");
        }
    }

    /// <summary>
    /// Whether an item is included and not excluded. A <c>scripts</c> selector also matches every script below that
    /// folder, and <c>skills</c> every agent skill, which <c>all</c> leaves out.
    /// </summary>
    public bool Includes(string item) =>
        (IsSkill(item) ? _includes.Contains(Skills) : _includes.Any(selector => Matches(item, selector))) &&
        !_excludes.Any(selector => Matches(item, selector));

    private static bool IsSkill(string item) => item.StartsWith(SkillsPath, StringComparison.Ordinal);

    private static string[] Parse(string? value, bool defaultAll) => string.IsNullOrWhiteSpace(value)
        ? defaultAll ? ["all"] : []
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Replace('\\', '/').TrimEnd('/').ToLowerInvariant()).ToArray();

    private static bool Matches(string item, string selector) => selector == "all" ||
        (selector == Skills && IsSkill(item)) ||
        string.Equals(item, selector, StringComparison.OrdinalIgnoreCase) ||
        (selector.StartsWith("scripts", StringComparison.OrdinalIgnoreCase) &&
         item.StartsWith(selector + "/", StringComparison.OrdinalIgnoreCase));
}
