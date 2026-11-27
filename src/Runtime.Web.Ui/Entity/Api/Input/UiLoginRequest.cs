namespace Anarchy.Toolkit.Runtime.Web.Ui.Entity.Api.Input;

/// <summary>
/// Generic login request for UI applications.
/// Contains client credentials for authentication.
/// </summary>
/// <param name="ClientId">The OAuth client id entered by the user.</param>
/// <param name="ClientSecret">The OAuth client secret entered by the user.</param>
/// <param name="TargetEnvironment">
/// The environment selected on the login page; only honoured on local hosts, otherwise the host's environment is used.
/// </param>
public sealed record UiLoginRequest(
    string ClientId,
    string ClientSecret,
    string? TargetEnvironment);
