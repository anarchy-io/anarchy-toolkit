using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>Resolves <see cref="ScaffoldOptions.Items"/> and <see cref="ScaffoldOptions.Exclude"/> against the available items.</summary>
internal sealed class ScaffoldSelection
{
    private static readonly string[] GeneratedItems = ["solution", "core", "tests", "samples", "docs", "Directory.Build.props"];
    private readonly string[] _includes;
    private readonly string[] _excludes;

    /// <exception cref="ArgumentException">A selector matches no available item.</exception>
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

    public bool Includes(string item) =>
        _includes.Any(selector => Matches(item, selector)) && !_excludes.Any(selector => Matches(item, selector));

    private static string[] Parse(string? value, bool defaultAll) => string.IsNullOrWhiteSpace(value)
        ? defaultAll ? ["all"] : []
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Replace('\\', '/').TrimEnd('/').ToLowerInvariant()).ToArray();

    private static bool Matches(string item, string selector) => selector == "all" ||
        string.Equals(item, selector, StringComparison.OrdinalIgnoreCase) ||
        (selector.StartsWith("scripts", StringComparison.OrdinalIgnoreCase) &&
         item.StartsWith(selector + "/", StringComparison.OrdinalIgnoreCase));
}
