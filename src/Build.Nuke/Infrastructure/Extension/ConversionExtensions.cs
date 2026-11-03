using Nuke.Common;

namespace Anarchy.Toolkit.Build.Nuke.Infrastructure.Extension
{
    /// <summary>
    /// Utility functions for conversion.
    /// </summary>
    public static class ConversionExtensions
    {
        /// <summary>
        /// Converts the specified value to <typeparamref name="T"/>. This is short-hand for <code>(build as T)</code>,
        /// but because it is an extension method it can be chained.
        /// </summary>
        public static T As<T>(this INukeBuild build)
            where T : class, INukeBuild =>
            (T)build;
    }
}
