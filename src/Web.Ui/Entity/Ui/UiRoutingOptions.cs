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

    /// <summary>Route prefix of the Blazor pages.</summary>
    public string UiRoutePrefix { get; init; } = "ui";

    /// <summary>Route prefix of the HTTP endpoints (login, logout, ...).</summary>
    public string EndpointRoutePrefix { get; init; } = "endpoint";
}
