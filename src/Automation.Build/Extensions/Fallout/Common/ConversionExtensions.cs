using Fallout.Common;

namespace Anarchy.Toolkit.Automation.Build.Extensions.Fallout.Common;

/// <summary>
/// Utility functions for conversion.
/// </summary>
/// <remarks>
/// Anarchy build components are interfaces with default members, which can only be reached through the interface
/// type; <see cref="As{T}"/> makes that cast readable.
/// </remarks>
public static class ConversionExtensions
{
    /// <summary>
    /// Casts the specified build to <typeparamref name="T"/>. This is shorthand for <c>(T)build</c>,
    /// but because it is an extension method it can be chained.
    /// </summary>
    /// <exception cref="InvalidCastException">
    /// Thrown when <paramref name="build"/> does not implement <typeparamref name="T"/>.
    /// </exception>
    /// <typeparam name="T">The build component interface to cast to.</typeparam>
    /// <param name="build">The build to cast.</param>
    /// <returns><paramref name="build"/> as <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code language="csharp">
    /// var layout = build.As&lt;ILayeringBuild&gt;().Layering;
    /// </code>
    /// </example>
    public static T As<T>(this IFalloutBuild build)
        where T : class, IFalloutBuild =>
        (T)build;
}
