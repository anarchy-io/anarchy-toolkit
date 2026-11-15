namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// Anarchy's logo as an <see cref="IConsoleBanner"/> (see <see cref="ConsoleBanner"/>). Derive from it to adjust the
/// output, or implement <see cref="IConsoleBanner"/> to replace it entirely.
/// </summary>
public class AnarchyConsoleBanner : IConsoleBanner
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static AnarchyConsoleBanner Default { get; } = new();

    /// <inheritdoc />
    public virtual string Render(int columns, bool color = true) => ConsoleBanner.Render(columns, color);
}
