using Humanizer;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Core.Extension;

/// <summary>
/// Extension methods for <see cref="TimeSpan"/>.
/// </summary>
public static class TimeSpanExtension
{
    /// <param name="span">The extended <see cref="TimeSpan"/>.</param>
    extension(TimeSpan span)
    {
        /// <summary>
        /// Returns the smaller of this span and <paramref name="spanB"/>.
        /// </summary>
        /// <param name="spanB">The span to compare with; when <see langword="null"/>, this span is returned.</param>
        /// <returns>The minimum of the two spans.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan Min(TimeSpan? spanB) => spanB.HasValue && spanB.Value < span ? spanB.Value : span;

        /// <summary>
        /// Formats the span as human-readable English text using Humanizer.
        /// </summary>
        /// <param name="maxUnit">The largest unit to use. Defaults to <see cref="TimeUnit.Year"/>.</param>
        /// <param name="minUnit">The smallest unit to use. Defaults to <see cref="TimeUnit.Second"/>.</param>
        /// <param name="precision">The maximum number of units to output. Defaults to 3.</param>
        /// <returns>A string such as <c>"1 hour, 2 minutes, 3 seconds"</c>.</returns>
        public string ToHuman(TimeUnit maxUnit = TimeUnit.Year, TimeUnit minUnit = TimeUnit.Second, int precision = 3)
        {
            return span.Humanize(maxUnit: maxUnit, minUnit: minUnit, precision: precision);
        }
    }
}
