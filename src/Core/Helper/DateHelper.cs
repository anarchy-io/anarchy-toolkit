using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using System.Globalization;

namespace Anarchy.Toolkit.Core.Helper;

public static class DateHelper
{
    private const int WinterTimeOffset = 1;
    private const int SummerTimeOffset = 2;

    private static TimeZoneInfo? _centralEuropeanTimeZoneInfo;

    public static readonly string GetIsoDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

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

    public static TimeSpan GetCentralEuropeanOffSet(DateTime dateTime)
    {
        return TimeSpan.FromHours(GetCentralEuropeanTimeZoneInfo().IsDaylightSavingTime(dateTime) ?
            SummerTimeOffset :
            WinterTimeOffset);
    }


    public static DateTimeOffset? ConvertToCentralEuropeanOffset(DateTime? dateTime)
    {
        if (dateTime == null)
        {
            return null;
        }

        return ConvertToCentralEuropeanOffset(new DateTimeOffset(dateTime.Value));
    }

    public static DateTimeOffset ConvertToCentralEuropeanOffset(DateTime dateTime)
    {
        return ConvertToCentralEuropeanOffset(new DateTimeOffset(dateTime));
    }

    public static DateTimeOffset? ConvertToCentralEuropeanOffset(DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset == null)
        {
            return null;
        }

        return ConvertToCentralEuropeanOffset(dateTimeOffset.Value);
    }

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

    public static DateTimeOffset? SanitiseDate(DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset == null)
        {
            return null;
        }

        // Strip time from date and return as UTC
        return SanitiseDate(dateTimeOffset.Value);
    }

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

    public static DateTimeOffset Now()
    {
        return DateTimeOffset.UtcNow;
    }

    public static TimeSpan Diff(DateTimeOffset reference)
    {
        return DateTimeOffset.UtcNow.Subtract(reference);
    }

    public static string ToHumanDiff(DateTimeOffset input)
    {
        return Diff(input).ToHuman();
    }
}
