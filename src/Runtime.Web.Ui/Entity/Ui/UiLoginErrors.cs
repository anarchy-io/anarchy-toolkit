namespace Anarchy.Toolkit.Runtime.Web.Ui.Entity.Ui;

/// <summary>
/// Error codes the login endpoint passes back to the login page in the <c>error</c> query parameter.
/// </summary>
public static class UiLoginErrors
{
    /// <summary>Name of the query parameter carrying the error code.</summary>
    public const string QueryParameter = "error";
    /// <summary>The client id or client secret was missing.</summary>
    public const string InvalidCredentials = "invalid_credentials";
    /// <summary>The auth service rejected the credentials or returned no token.</summary>
    public const string AuthFailed = "auth_failed";
}
