using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

namespace Anarchy.Toolkit.Web.Service.SwaggerGen;

/// <summary>
/// Builds the HTML description shown in the Swagger UI.
/// </summary>
/// <remarks>
/// Domain-specific links (repository, dashboards, docs, chat channel) are not
/// hardcoded. Consumers can supply their own links through
/// <see cref="SwaggerDescriptionOptions"/>; any link left null is omitted.
/// </remarks>
public static class SwaggerDescriptionBuilder
{
    private const string RuntimeTemplate = """
        <h4>⚙️ Runtime details</h4>
        <ul>
            <li><b>Environment:</b> <span>{Environment}</span></li>
            <li><b>OAuth2 authority:</b> <span>{AuthHost}</span></li>
        </ul>
        """;

    /// <summary>
    /// Builds the HTML description containing the runtime details (environment name and OAuth2 authority) and the
    /// optional resource links and team credit.
    /// </summary>
    /// <param name="appSettings">Auth service settings providing the OAuth2 authority.</param>
    /// <param name="environment">The host environment whose name is displayed.</param>
    /// <param name="options">Optional links; when <see langword="null"/>, only runtime details are rendered.</param>
    /// <returns>The HTML description.</returns>
    public static string Build(IAuthServiceAppSettings appSettings,
                               IHostEnvironment environment,
                               SwaggerDescriptionOptions? options = null)
    {
        var description = RuntimeTemplate
            .Replace("{Environment}", environment.EnvironmentName)
            .Replace("{AuthHost}", appSettings.AuthService.Host.ToString());

        options ??= new SwaggerDescriptionOptions();

        var resources = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.RepositoryUrl))
            resources.Add($"<li>💻 <a href='{options.RepositoryUrl}'>Codebase</a></li>");
        if (!string.IsNullOrWhiteSpace(options.DashboardUrl))
            resources.Add($"<li>📊 <a href='{options.DashboardUrl}'>Dashboard</a></li>");
        if (!string.IsNullOrWhiteSpace(options.DocumentationUrl))
            resources.Add($"<li>📄 <a href='{options.DocumentationUrl}'>Documentation</a></li>");

        if (resources.Count != 0)
        {
            description += $"<h4>📘 Resources</h4><ul>{string.Join(string.Empty, resources)}</ul>";
        }

        if (!string.IsNullOrWhiteSpace(options.TeamName) && !string.IsNullOrWhiteSpace(options.TeamLink))
        {
            description += $"<p>Built with ❤️ <i>by <a href='{options.TeamLink}'>{options.TeamName}</a>.</i></p>";
        }

        return description + "<br/>";
    }
}

/// <summary>
/// Optional, consumer-supplied links rendered in the Swagger description.
/// </summary>
public record SwaggerDescriptionOptions
{
    /// <summary>URL of the source code repository, rendered as "Codebase".</summary>
    public string? RepositoryUrl { get; init; }
    /// <summary>URL of the monitoring dashboard, rendered as "Dashboard".</summary>
    public string? DashboardUrl { get; init; }
    /// <summary>URL of the documentation, rendered as "Documentation".</summary>
    public string? DocumentationUrl { get; init; }
    /// <summary>Name of the owning team; the credit line is rendered only when <see cref="TeamLink"/> is also set.</summary>
    public string? TeamName { get; init; }
    /// <summary>Link to the owning team; the credit line is rendered only when <see cref="TeamName"/> is also set.</summary>
    public string? TeamLink { get; init; }
}
