namespace Anarchy.Toolkit.Runtime.Web.Swagger.Entity;

/// <summary>
/// The Swagger UI theme of toolkit REST APIs: a colours stylesheet with light and dark values, a theme stylesheet that
/// reads only those colours, a script remembering the light/dark choice, a header logo for each mode, and a favicon. Enabled by default with the
/// Anarchy palette.
/// </summary>
/// <remarks>
/// <para>
/// Read by <c>UseToolkitSwagger</c> through <c>IOptions&lt;SwaggerThemeOptions&gt;</c>; configure it with
/// <c>services.Configure&lt;SwaggerThemeOptions&gt;(...)</c>.
/// </para>
/// <para>
/// The default stylesheets and script are embedded in this package, the logos and favicons in
/// <c>Anarchy.Toolkit.Runtime.Web</c>; both follow the usual <c>wwwroot</c> layout (<c>css/</c>, <c>js/</c>,
/// <c>images/</c>, favicons at the root). Files in the API project's own <c>wwwroot</c> are served first,
/// so adding <c>wwwroot/css/swagger-colors.css</c> rebrands the UI without writing a theme. Relative paths are
/// resolved against the application base path (prefixed with <c>/{EnvironmentName}</c> outside the local
/// environment); absolute URLs and paths starting with <c>/</c> are used as given.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// // Keep Swagger UI's own look
/// services.Configure&lt;SwaggerThemeOptions&gt;(theme =&gt; theme.Enabled = false);
///
/// // Keep the theme, with your own colours
/// services.Configure&lt;SwaggerThemeOptions&gt;(theme =&gt; theme.ColorsStylesheet = "css/my-swagger-colors.css");
/// </code>
/// </example>
public sealed class SwaggerThemeOptions
{
    /// <summary>
    /// Whether the stylesheets and favicon are added to Swagger UI. When <see langword="false"/>, Swagger UI keeps
    /// its own look.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The stylesheet defining the <c>--swagger-theme-*</c> colour, font and shape variables.
    /// </summary>
    /// <value>Defaults to <c>css/swagger-colors.css</c> (the Anarchy I/O palette).</value>
    public string ColorsStylesheet { get; set; } = "css/swagger-colors.css";

    /// <summary>
    /// The stylesheet applying the variables of <see cref="ColorsStylesheet"/> to Swagger UI; <see langword="null"/>
    /// adds none.
    /// </summary>
    /// <value>Defaults to <c>css/swagger-theme.css</c>.</value>
    public string? Stylesheet { get; set; } = "css/swagger-theme.css";

    /// <summary>
    /// The script remembering the light/dark choice made with Swagger UI's header toggle; <see langword="null"/> adds
    /// none, leaving Swagger UI to follow the system preference on every load.
    /// </summary>
    /// <value>Defaults to <c>js/swagger-theme.js</c>.</value>
    public string? Script { get; set; } = "js/swagger-theme.js";

    /// <summary>
    /// The logo replacing Swagger's in the header, in light mode (and in dark mode without a
    /// <see cref="DarkLogo"/>); <see langword="null"/> keeps Swagger's logo.
    /// </summary>
    /// <value>Defaults to <c>images/logo.png</c> (the Anarchy I/O logo with dark text).</value>
    public string? Logo { get; set; } = "images/logo.png";

    /// <summary>
    /// The header logo in dark mode; <see langword="null"/> uses <see cref="Logo"/>.
    /// </summary>
    /// <value>Defaults to <c>images/logo-dark.png</c> (the Anarchy I/O logo with white text).</value>
    public string? DarkLogo { get; set; } = "images/logo-dark.png";

    /// <summary>
    /// The favicon of the Swagger UI page; <see langword="null"/> adds none. An <c>.svg</c> is declared as
    /// <c>image/svg+xml</c>, so browsers without SVG favicons use <see cref="FallbackFavicon"/>.
    /// </summary>
    /// <value>Defaults to <c>favicon.svg</c> (the Anarchy symbol).</value>
    public string? Favicon { get; set; } = "favicon.svg";

    /// <summary>
    /// The raster favicon for browsers that do not support <see cref="Favicon"/>; <see langword="null"/> adds none.
    /// </summary>
    /// <value>Defaults to <c>favicon.png</c> (the Anarchy symbol, 64×64).</value>
    public string? FallbackFavicon { get; set; } = "favicon.png";
}
