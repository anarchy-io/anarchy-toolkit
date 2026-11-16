using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using System.Globalization;

namespace Anarchy.Toolkit.Core.Helper;

/// <summary>
/// Date and time helpers: Central European Time (CET/CEST) conversions, date sanitisation, ISO-8601 normalisation and
/// "time ago" computations.
/// </summary>
/// <remarks>
/// Most methods are also exposed as extension methods through <see cref="DateTimeExtensions"/>. The Central European
/// <see cref="TimeZoneInfo"/> is resolved lazily and cached (the cache is not synchronised, but concurrent first calls
/// resolve the same value).
/// </remarks>
public static class DateHelper
{
    private const int WinterTimeOffset = 1;
    private const int SummerTimeOffset = 2;

    private static TimeZoneInfo? _centralEuropeanTimeZoneInfo;

    /// <summary>
    /// The ISO-8601 UTC format with milliseconds used throughout the toolkit: <c>yyyy-MM-ddTHH:mm:ss.fffZ</c>.
    /// </summary>
    public static readonly string GetIsoDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <summary>
    /// Gets the Central European time zone, trying the Windows id (<c>Central European Standard Time</c>) first and
    /// falling back to the IANA id (<c>Europe/Amsterdam</c>).
    /// </summary>
    /// <returns>The cached Central European <see cref="TimeZoneInfo"/>.</returns>
    /// <exception cref="TimeZoneNotFoundException">Neither time zone id is available on the host.</exception>
    public static TimeZoneInfo GetCentralEuropeanTimeZoneInfo()
    {
        const string windowsCetTime = "Central European Standard Time";
        const string amsterdamTime = "Europe/Amsterdam";

        if (_centralEuropeanTimeZoneInfo is not null)
        {
            return _centralEuropeanTimeZoneInfo;
        }

        try
        {
            _centralEuropeanTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(windowsCetTime);
        }
        catch (TimeZoneNotFoundException)
        {
            _centralEuropeanTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(amsterdamTime);
        }

        return _centralEuropeanTimeZoneInfo;
    }

    /// <summary>
    /// Gets the Central European UTC offset for <paramref name="dateTime"/>: +2h when daylight saving time applies
    /// (CEST), +1h otherwise (CET).
    /// </summary>
    /// <param name="dateTime">The date/time to evaluate.</param>
    /// <returns>The offset from UTC.</returns>
    public static TimeSpan GetCentralEuropeanOffSet(DateTime dateTime)
    {
        return TimeSpan.FromHours(GetCentralEuropeanTimeZoneInfo().IsDaylightSavingTime(dateTime) ?
            SummerTimeOffset :
            WinterTimeOffset);
    }


    /// <summary>
    /// Nullable overload of <see cref="ConvertToCentralEuropeanOffset(DateTime)"/>.
    /// </summary>
    /// <param name="dateTime">The value to convert; may be <see langword="null"/>.</param>
    /// <returns><see langword="null"/> when <paramref name="dateTime"/> is <see langword="null"/>; otherwise the converted value.</returns>
    public static DateTimeOffset? ConvertToCentralEuropeanOffset(DateTime? dateTime)
    {
        if (dateTime == null)
        {
            return null;
        }

        return ConvertToCentralEuropeanOffset(new DateTimeOffset(dateTime.Value));
    }

    /// <summary>
    /// Converts <paramref name="dateTime"/> to a <see cref="DateTimeOffset"/> and re-labels it with the Central
    /// European offset. See <see cref="ConvertToCentralEuropeanOffset(DateTimeOffset)"/>.
    /// </summary>
    /// <param name="dateTime">The value to convert.</param>
    /// <returns>The value with the Central European offset.</returns>
    public static DateTimeOffset ConvertToCentralEuropeanOffset(DateTime dateTime)
    {
        return ConvertToCentralEuropeanOffset(new DateTimeOffset(dateTime));
    }

    /// <summary>
    /// Nullable overload of <see cref="ConvertToCentralEuropeanOffset(DateTimeOffset)"/>.
    /// </summary>
    /// <param name="dateTimeOffset">The value to convert; may be <see langword="null"/>.</param>
    /// <returns><see langword="null"/> when <paramref name="dateTimeOffset"/> is <see langword="null"/>; otherwise the converted value.</returns>
    public static DateTimeOffset? ConvertToCentralEuropeanOffset(DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset == null)
        {
            return null;
        }

        return ConvertToCentralEuropeanOffset(dateTimeOffset.Value);
    }

    /// <summary>
    /// Keeps the wall-clock components (year to millisecond) of <paramref name="dateTimeOffset"/> and replaces its
    /// offset with the Central European offset applicable to that wall-clock time.
    /// </summary>
    /// <remarks>
    /// This does <b>not</b> convert the instant: the clock time is preserved and only the offset changes, so the
    /// resulting instant differs from the input unless the input already had the Central European offset.
    /// </remarks>
    /// <param name="dateTimeOffset">The value to re-label.</param>
    /// <returns>The value with the Central European offset.</returns>
    public static DateTimeOffset ConvertToCentralEuropeanOffset(DateTimeOffset dateTimeOffset)
    {
        // Get the local time zone's offset for the specified DateTime
        var dateTime = new DateTime(
            dateTimeOffset.Year,
            dateTimeOffset.Month,
            dateTimeOffset.Day,
            dateTimeOffset.Hour,
            dateTimeOffset.Minute,
            dateTimeOffset.Second,
            dateTimeOffset.Millisecond,
            DateTimeKind.Unspecified);

        var offset = GetCentralEuropeanOffSet(dateTime);
        var newLocalDate = new DateTimeOffset(dateTime, offset);

        return newLocalDate;
    }

    /// <summary>
    /// Nullable overload of <see cref="SanitiseDate(DateTimeOffset)"/>.
    /// </summary>
    /// <param name="dateTimeOffset">The value to sanitise; may be <see langword="null"/>.</param>
    /// <returns><see langword="null"/> when <paramref name="dateTimeOffset"/> is <see langword="null"/>; otherwise the sanitised date.</returns>
    public static DateTimeOffset? SanitiseDate(DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset == null)
        {
            return null;
        }

        // Strip time from date and return as UTC
        return SanitiseDate(dateTimeOffset.Value);
    }

    /// <summary>
    /// Strips the time component, returning midnight UTC of the same calendar day as shown by
    /// <paramref name="dateTimeOffset"/> (the date is taken in its own offset, not converted to UTC first).
    /// </summary>
    /// <param name="dateTimeOffset">The value to sanitise.</param>
    /// <returns>A <see cref="DateTimeOffset"/> at 00:00:00 with a zero offset.</returns>
    public static DateTimeOffset SanitiseDate(DateTimeOffset dateTimeOffset)
    {
        // Strip time from date and return as UTC
        return new DateTimeOffset(
            new DateTime(
                dateTimeOffset.Year,
                dateTimeOffset.Month,
                dateTimeOffset.Day,
                0,
                0,
                0,
                DateTimeKind.Utc), TimeSpan.Zero);
    }

    /// <summary>
    /// Normalises a date/time string in one of the known formats into <see cref="GetIsoDateTimeFormat"/>.
    /// </summary>
    /// <remarks>
    /// Accepted formats (invariant culture, values without offset are assumed UTC): <c>dd-MM-yyyy HH:mm:ss</c>,
    /// <c>dd-MMM-yy</c>, <c>dd-MM-yyyy</c>, <c>dd/MM/yyyy</c>, and ISO-8601 with or without milliseconds and with
    /// <c>Z</c> or an explicit offset. Input already in <see cref="GetIsoDateTimeFormat"/> is returned unchanged.
    /// </remarks>
    /// <param name="dateTime">The date/time string to normalise.</param>
    /// <returns>The value formatted as <c>yyyy-MM-ddTHH:mm:ss.fffZ</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dateTime"/> is null, empty or whitespace.</exception>
    /// <exception cref="InvalidFormatException"><paramref name="dateTime"/> matches none of the known formats.</exception>
    public static string GetIsoFormattedDateTime(string dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime))
        {
            throw new ArgumentNullException(nameof(dateTime));
        }

        string[] knownFormats = [
            "dd-MM-yyyy HH:mm:ss", // e.g., 28-09-2023 00:00:00
            "dd-MMM-yy", // e.g., 01-JUN-24
            "dd-MM-yyyy", // e.g., 28-09-2023
            "dd/MM/yyyy", // e.g., 25/06/2024
            "yyyy-MM-ddTHH:mm:ss.fffZ", // ISO 8601 format with milliseconds
            "yyyy-MM-ddTHH:mm:ss.fffzzz", // ISO 8601 format with milliseconds and timezone
            "yyyy-MM-ddTHH:mm:ssZ", // ISO 8601 format without milliseconds
            "yyyy-MM-ddTHH:mm:sszzz" // ISO 8601 format without milliseconds with timezone
        ];

        if (DateTimeOffset.TryParseExact(dateTime, GetIsoDateTimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out _))
        {
            return dateTime;
        }

        foreach (var format in knownFormats)
        {
            if (DateTimeOffset.TryParseExact(dateTime, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var parsedDateTime))
            {
                return parsedDateTime.ToString(GetIsoDateTimeFormat);
            }
        }

        throw new InvalidFormatException(dateTime, typeof(DateTimeOffset));
    }

    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    /// <returns><see cref="DateTimeOffset.UtcNow"/>.</returns>
    public static DateTimeOffset Now()
    {
        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the time elapsed since <paramref name="reference"/> (negative when it lies in the future).
    /// </summary>
    /// <param name="reference">The reference point in time.</param>
    /// <returns><c>DateTimeOffset.UtcNow - reference</c>.</returns>
    public static TimeSpan Diff(DateTimeOffset reference)
    {
        return DateTimeOffset.UtcNow.Subtract(reference);
    }

    /// <summary>
    /// Describes the time elapsed since <paramref name="input"/> in human-readable English.
    /// </summary>
    /// <param name="input">The reference point in time.</param>
    /// <returns>The elapsed time formatted with <see cref="TimeSpanExtension"/>'s <c>ToHuman</c>.</returns>
    public static string ToHumanDiff(DateTimeOffset input)
    {
        return Diff(input).ToHuman();
    }
}
