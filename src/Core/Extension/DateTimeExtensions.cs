using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Core.Extension;

public static class DateTimeExtensions
{
    public static TimeSpan GetCentralEuropeanOffSet(this DateTime dateTime)
    {
        return DateHelper.GetCentralEuropeanOffSet(dateTime);
    }

    public static DateTimeOffset? ConvertToCentralEuropeanOffset(this DateTime? dateTime)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTime);
    }

    public static DateTimeOffset ConvertToCentralEuropeanOffset(this DateTime dateTime)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTime);
    }

    public static DateTimeOffset? ConvertToCentralEuropeanOffset(this DateTimeOffset? dateTimeOffset)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTimeOffset);
    }

    public static DateTimeOffset ConvertToCentralEuropeanOffset(this DateTimeOffset dateTimeOffset)
    {
        return DateHelper.ConvertToCentralEuropeanOffset(dateTimeOffset);
    }

    public static DateTimeOffset? SanitiseDate(this DateTimeOffset? dateTimeOffset)
    {
        return DateHelper.SanitiseDate(dateTimeOffset);
    }

    extension(DateTimeOffset dateTimeOffset)
    {
        public DateTimeOffset SanitiseDate()
        {
            return DateHelper.SanitiseDate(dateTimeOffset);
        }

        public string ToHumanDiff()
        {
            return DateHelper.ToHumanDiff(dateTimeOffset);
        }
    }
}
