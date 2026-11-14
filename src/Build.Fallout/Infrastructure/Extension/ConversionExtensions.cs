using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;

/// <summary>
/// Utility functions for conversion.
/// </summary>
public static class ConversionExtensions
{
    /// <summary>
    /// Casts the specified build to <typeparamref name="T"/>. This is shorthand for <c>(T)build</c>,
    /// but because it is an extension method it can be chained.
    /// </summary>
    /// <exception cref="InvalidCastException">
    /// Thrown when <paramref name="build"/> does not implement <typeparamref name="T"/>.
    /// </exception>
    public static T As<T>(this IFalloutBuild build)
        where T : class, IFalloutBuild =>
        (T)build;
}
