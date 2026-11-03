namespace Anarchy.Toolkit.Web.Ui.Entity.Ui;

/// <summary>
/// Error codes the login endpoint passes back to the login page in the <c>error</c> query parameter.
/// </summary>
public static class UiLoginErrors
{
    public const string QueryParameter = "error";
    public const string InvalidCredentials = "invalid_credentials";
    public const string AuthFailed = "auth_failed";
}
