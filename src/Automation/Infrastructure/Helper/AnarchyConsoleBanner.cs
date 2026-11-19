namespace Anarchy.Toolkit.Automation.Infrastructure.Helper;

/// <summary>
/// Anarchy's logo as an <see cref="IConsoleBanner"/> (see <see cref="ConsoleBanner"/>). Derive from it to adjust the
/// output, or implement <see cref="IConsoleBanner"/> to replace it entirely.
/// </summary>
/// <example>
/// <code language="csharp">
/// public sealed class QuietBanner : AnarchyConsoleBanner
/// {
///     public override string Render(int columns, bool color = true) =&gt; base.Render(columns, color: false);
/// }
///
/// var options = new ConsoleBannerOptions { Banner = new QuietBanner() };
/// </code>
/// </example>
public class AnarchyConsoleBanner : IConsoleBanner
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static AnarchyConsoleBanner Default { get; } = new();

    /// <inheritdoc />
    public virtual string Render(int columns, bool color = true) => ConsoleBanner.Render(columns, color);
}
