namespace Anarchy.Toolkit.Runtime.Web.Ui.Core.Service.Ui.Session;

/// <summary>
/// Read-only view of the signed-in UI session (backed by the auth cookie).
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddToolkitUiAuthentication</c>. Used by <c>UiApiClientBase</c> to
/// authenticate downstream calls and select the target environment.
/// </remarks>
public interface IUiSessionContext
{
    /// <summary>Whether the current user is authenticated.</summary>
    /// <returns><see langword="true"/> when the user has an authenticated identity.</returns>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>The OAuth access token issued at login, used to call downstream APIs.</summary>
    /// <returns>The access token, or <see langword="null"/> when not signed in.</returns>
    Task<string?> GetAccessTokenAsync();

    /// <summary>The client id the user logged in with.</summary>
    /// <returns>The client id, or <see langword="null"/> when not signed in.</returns>
    Task<string?> GetClientNameAsync();

    /// <summary>The environment selected at login, or the host's own environment.</summary>
    /// <returns>The environment name.</returns>
    Task<string> GetTargetEnvironmentAsync();
}
