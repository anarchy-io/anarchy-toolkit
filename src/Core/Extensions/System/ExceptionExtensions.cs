namespace Anarchy.Toolkit.Core.Extensions.System;

/// <summary>
/// Extension methods for <see cref="Exception"/>.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Returns the fully qualified name of the exception type if available,
    /// otherwise falls back to the Source property, and finally to null.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>
    /// The exception's type full name, its <see cref="Exception.Source"/>, or <see langword="null"/>. Never throws.
    /// </returns>
    public static string? GetQualifiedSource(this Exception ex)
    {
        try
        {
            return ex switch {
                not null when !string.IsNullOrWhiteSpace(ex.GetType().FullName) => $"{ex.GetType().FullName}",
                not null when !string.IsNullOrWhiteSpace(ex.Source) => ex.Source,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
