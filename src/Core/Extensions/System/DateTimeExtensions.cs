using Anarchy.Toolkit.Core.Helpers.Dates;

namespace Anarchy.Toolkit.Core.Extensions.System;

/// <summary>
/// Extension methods over <see cref="DateTime"/> and <see cref="DateTimeOffset"/> for Central European Time (CET/CEST)
/// conversions, date sanitisation and human-readable time differences.
/// </summary>
/// <remarks>
/// Every method is a thin wrapper around the equivalent static method in <see cref="DateHelper"/>; see that class for
/// the exact conversion rules.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var cet = DateTime.UtcNow.ConvertToCentralEuropeanOffset();
/// var dayOnly = DateTimeOffset.Now.SanitiseDate();      // midnight UTC of the same calendar day
/// var ago = DateTimeOffset.UtcNow.AddMinutes(-5).ToHumanDiff(); // e.g. "5 minutes"
/// </code>
/// </example>
public static class DateTimeExtensions
{
    /// <summary>
    /// Gets the Central European UTC offset (+1h in winter, +2h in summer) that applies to <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date/time to evaluate against the Central European daylight saving rules.</param>
    /// <returns>The offset from UTC that applies to <paramref name="dateTime"/> in Central Europe.</returns>
    /// <seealso cref="DateHelper.GetCentralEuropeanOffSet(DateTime)"/>
    public static TimeSpan GetCentralEuropeanOffSet(this DateTime dateTime)
    {
        return DateHelper.GetCentralEuropeanOffSet(dateTime);
    }

    /// <summary>
    /// Re-labels a nullable <see cref="DateTime"/> with the Central European offset that applies to it.
    /// </summary>
    /// <param name="dateTime">The date/time to convert; may be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="null"/> when <paramref name="dateTime"/> is <see langword="null"/>; otherwise the converted value.
    /// </returns>
    /// <seealso cref="DateHelper.ConvertToCentralEuropeanOffset(DateTimeOffset)"/>
    public static DateTimeOffset? ConvertToCentralEuropeanOffset(this DateTime? dateTime)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTime);
    }

    /// <summary>
    /// Re-labels a <see cref="DateTime"/> with the Central European offset that applies to it.
    /// </summary>
    /// <param name="dateTime">The date/time to convert.</param>
    /// <returns>A <see cref="DateTimeOffset"/> with the same wall-clock components and the Central European offset.</returns>
    /// <seealso cref="DateHelper.ConvertToCentralEuropeanOffset(DateTimeOffset)"/>
    public static DateTimeOffset ConvertToCentralEuropeanOffset(this DateTime dateTime)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTime);
    }

    /// <summary>
    /// Re-labels a nullable <see cref="DateTimeOffset"/> with the Central European offset that applies to it.
    /// </summary>
    /// <param name="dateTimeOffset">The value to convert; may be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="null"/> when <paramref name="dateTimeOffset"/> is <see langword="null"/>; otherwise the converted
    /// value.
    /// </returns>
    /// <seealso cref="DateHelper.ConvertToCentralEuropeanOffset(DateTimeOffset)"/>
    public static DateTimeOffset? ConvertToCentralEuropeanOffset(this DateTimeOffset? dateTimeOffset)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTimeOffset);
    }

    /// <summary>
    /// Re-labels a <see cref="DateTimeOffset"/> with the Central European offset that applies to it.
    /// </summary>
    /// <param name="dateTimeOffset">The value to convert.</param>
    /// <returns>A value with the same wall-clock components and the Central European offset.</returns>
    /// <seealso cref="DateHelper.ConvertToCentralEuropeanOffset(DateTimeOffset)"/>
    public static DateTimeOffset ConvertToCentralEuropeanOffset(this DateTimeOffset dateTimeOffset)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTimeOffset);
    }

    /// <summary>
    /// Strips the time component of a nullable <see cref="DateTimeOffset"/>, returning midnight UTC of the same
    /// calendar day.
    /// </summary>
    /// <param name="dateTimeOffset">The value to sanitise; may be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="null"/> when <paramref name="dateTimeOffset"/> is <see langword="null"/>; otherwise the sanitised
    /// date.
    /// </returns>
    /// <seealso cref="DateHelper.SanitiseDate(DateTimeOffset)"/>
    public static DateTimeOffset? SanitiseDate(this DateTimeOffset? dateTimeOffset)
    {
        return DateHelper.SanitiseDate(dateTimeOffset);
    }

    /// <param name="dateTimeOffset">The extended <see cref="DateTimeOffset"/> value.</param>
    extension(DateTimeOffset dateTimeOffset)
    {
        /// <summary>
        /// Strips the time component, returning midnight UTC of the same calendar day (year/month/day are taken as-is,
        /// without converting to UTC first).
        /// </summary>
        /// <returns>A <see cref="DateTimeOffset"/> at 00:00:00 with a zero offset.</returns>
        /// <seealso cref="DateHelper.SanitiseDate(DateTimeOffset)"/>
        public DateTimeOffset SanitiseDate()
        {
            return DateHelper.SanitiseDate(dateTimeOffset);
        }

        /// <summary>
        /// Describes, in human-readable English, how much time elapsed between this value and now (UTC).
        /// </summary>
        /// <returns>A humanized time span such as <c>"2 hours, 3 minutes"</c>.</returns>
        /// <seealso cref="DateHelper.ToHumanDiff(DateTimeOffset)"/>
        public string ToHumanDiff()
        {
            return DateHelper.ToHumanDiff(dateTimeOffset);
        }
    }
}
