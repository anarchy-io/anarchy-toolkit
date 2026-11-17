namespace Anarchy.Toolkit.Web.Ui.Entity.Ui;

/// <summary>
/// How a UI application builds the links it renders.
/// </summary>
public record UiRoutingOptions
{
    /// <summary>
    /// Prefix every link with <c>/{environment-name}</c> on TAP hosts (e.g. when served behind an API gateway stage).
    /// Never applied on local hosts.
    /// </summary>
    public bool EnableEnvironmentNamePathPrefix { get; init; }

    /// <summary>
    /// Path the UI is mounted under (e.g. <c>/__mock/ui</c> for <c>app.Map("/__mock/ui", ...)</c>); prepended to every
    /// link before the environment prefix. Normalized to a leading slash and no trailing slash; empty for the root.
    /// </summary>
    public string PathBase {
        get;
        init {
            var trimmed = (value ?? string.Empty).Trim().Trim('/');
            field = trimmed.Length == 0 ? string.Empty : "/" + trimmed;
        }
    } = "";

    /// <summary>Route prefix of the Blazor pages.</summary>
    public string UiRoutePrefix { get; init; } = "ui";

    /// <summary>Route prefix of the HTTP endpoints (login, logout, ...).</summary>
    public string EndpointRoutePrefix { get; init; } = "endpoint";
}
