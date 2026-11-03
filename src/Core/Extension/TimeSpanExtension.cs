using Humanizer;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Core.Extension;

public static class TimeSpanExtension
{
    extension(TimeSpan span)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan Min(TimeSpan? spanB) => spanB.HasValue && spanB.Value < span ? spanB.Value : span;

        public string ToHuman(TimeUnit maxUnit = TimeUnit.Year, TimeUnit minUnit = TimeUnit.Second, int precision = 3)
        {
            return span.Humanize(maxUnit: maxUnit, minUnit: minUnit, precision: precision);
        }
    }
}
