namespace Anarchy.Toolkit.Core.Extension;

/// <summary>
/// Extension methods for numeric types.
/// </summary>
public static class NumberExtension
{
    /// <summary>
    /// Checks whether a nullable integer lies within an inclusive range.
    /// </summary>
    /// <param name="value">The value to test; <see langword="null"/> is never within range.</param>
    /// <param name="start">Inclusive lower bound.</param>
    /// <param name="end">Inclusive upper bound.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="value"/> has a value and
    /// <paramref name="start"/> &lt;= value &lt;= <paramref name="end"/>; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsBetween(this int? value, int start, int end)
    {
        return value.HasValue && value.Value >= start && value.Value <= end;
    }
}
