namespace Anarchy.Toolkit.Web.Ui.Core.Service.Ui.Session;

/// <summary>
/// Read-only view of the signed-in UI session (backed by the auth cookie).
/// </summary>
public interface IUiSessionContext
{
    Task<bool> IsAuthenticatedAsync();

    /// <summary>The OAuth access token issued at login, used to call downstream APIs.</summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>The client id the user logged in with.</summary>
    Task<string?> GetClientNameAsync();

    /// <summary>The environment selected at login, or the host's own environment.</summary>
    Task<string> GetTargetEnvironmentAsync();
}
