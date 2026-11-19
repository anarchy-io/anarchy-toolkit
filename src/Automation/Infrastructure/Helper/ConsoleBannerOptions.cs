using Spectre.Console;

namespace Anarchy.Toolkit.Automation.Infrastructure.Helper;

/// <summary>
/// Which banner a tool built on these packages prints: Anarchy's by default, another <see cref="IConsoleBanner"/>
/// through <see cref="Banner"/>, or none at all with <see cref="Hide"/>.
/// </summary>
/// <example>
/// <code language="csharp">
/// new ConsoleBannerOptions { Hide = true }.Write(AnsiConsole.Console);
/// </code>
/// </example>
public sealed record ConsoleBannerOptions
{
    /// <summary>
    /// The banner to print. Defaults to <see cref="AnarchyConsoleBanner.Default"/>.
    /// </summary>
    public IConsoleBanner Banner { get; init; } = AnarchyConsoleBanner.Default;

    /// <summary>
    /// When true, no banner is printed at all. Defaults to false.
    /// </summary>
    public bool Hide { get; init; }

    /// <summary>
    /// Writes <see cref="Banner"/> to <paramref name="console"/>, sized to its width and colored when it supports
    /// ANSI, unless <see cref="Hide"/> is set or the output is redirected (so piped or captured output stays plain).
    /// </summary>
    /// <param name="console">The console to write to.</param>
    public void Write(IAnsiConsole console)
    {
        if (Hide || Console.IsOutputRedirected) return;
        var writer = console.Profile.Out.Writer;
        writer.Write(Banner.Render(console.Profile.Width, color: console.Profile.Capabilities.Ansi));
        writer.Flush();
    }
}
