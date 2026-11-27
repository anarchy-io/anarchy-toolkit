namespace Anarchy.Toolkit.Automation.Helpers.Banners;

/// <summary>
/// Renders the banner shown at the top of a tool's output. Use <see cref="AnarchyConsoleBanner"/> to keep Anarchy's
/// logo, or implement it to show your own.
/// </summary>
public interface IConsoleBanner
{
    /// <summary>
    /// The banner for a terminal <paramref name="columns"/> wide, with ANSI colors unless <paramref name="color"/> is false.
    /// </summary>
    /// <param name="columns">The terminal width, in columns.</param>
    /// <param name="color">Whether to include ANSI color escape sequences.</param>
    /// <returns>The banner text, printed as-is.</returns>
    string Render(int columns, bool color = true);
}
