namespace Anarchy.Toolkit.Core.Helper;

/// <summary>
/// Helper class for date range validation operations.
/// </summary>
public static class DateRangeHelper
{
    /// <summary>
    /// Checks if a specific point in time falls within a date range.
    /// Truncates all values to milliseconds to avoid precision issues.
    /// </summary>
    /// <param name="from">The start of the range (inclusive).</param>
    /// <param name="to">The end of the range (exclusive). Null means indefinite.</param>
    /// <param name="at">The point in time to check. Defaults to current UTC time.</param>
    /// <returns>True if the specified time falls within the range.</returns>
    public static bool IsWithinRange(DateTimeOffset from, DateTimeOffset? to, DateTimeOffset? at = null)
    {
        var truncatedFrom = TruncateToMilliseconds(from);
        var truncatedTo = to.HasValue ? TruncateToMilliseconds(to.Value) : (DateTimeOffset?)null;
        var truncatedAt = TruncateToMilliseconds(at ?? DateHelper.Now());

        return truncatedFrom <= truncatedAt && (truncatedTo == null || truncatedTo > truncatedAt);
    }

    /// <summary>
    /// Checks if two date ranges overlap.
    /// </summary>
    /// <param name="range1From">Start of the first range.</param>
    /// <param name="range1To">End of the first range. Null means indefinite.</param>
    /// <param name="range2From">Start of the second range.</param>
    /// <param name="range2To">End of the second range. Null means indefinite.</param>
    /// <returns>True if the date ranges overlap.</returns>
    public static bool DoDateRangesOverlap(
        DateTimeOffset range1From,
        DateTimeOffset? range1To,
        DateTimeOffset range2From,
        DateTimeOffset? range2To)
    {
        var effectiveRange1To = TruncateToMilliseconds(range1To ?? DateTimeOffset.MaxValue);
        var effectiveRange2To = TruncateToMilliseconds(range2To ?? DateTimeOffset.MaxValue);
        var truncatedRange1From = TruncateToMilliseconds(range1From);
        var truncatedRange2From = TruncateToMilliseconds(range2From);

        return effectiveRange1To >= truncatedRange2From && truncatedRange1From <= effectiveRange2To;
    }

    /// <summary>
    /// Truncates a DateTimeOffset to millisecond precision to avoid precision issues
    /// when comparing dates from different sources (e.g., database vs. in-memory).
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset to truncate.</param>
    /// <returns>A DateTimeOffset truncated to millisecond precision.</returns>
    public static DateTimeOffset TruncateToMilliseconds(DateTimeOffset dateTime)
    {
        return new DateTimeOffset(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second,
            dateTime.Millisecond,
            dateTime.Offset);
    }
}
