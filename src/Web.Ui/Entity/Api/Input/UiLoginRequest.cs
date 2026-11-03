namespace Anarchy.Toolkit.Web.Ui.Entity.Api.Input;

/// <summary>
/// Generic login request for UI applications.
/// Contains client credentials for authentication.
/// </summary>
public sealed record UiLoginRequest(
    string ClientId,
    string ClientSecret,
    string? TargetEnvironment);
