namespace Anarchy.Toolkit.Core.Extension;

public static class ExceptionExtensions
{
    /// <summary>
    /// Returns the fully qualified name of the exception type if available,
    /// otherwise falls back to the Source property, and finally to null.
    /// </summary>
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
