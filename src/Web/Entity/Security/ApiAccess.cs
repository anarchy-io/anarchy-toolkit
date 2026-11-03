namespace Anarchy.Toolkit.Web.Entity.Security;

/// <summary>
/// Generic authorization policy names for toolkit-based REST APIs.
/// </summary>
/// <remarks>
/// Only generic policy names (Admin/Read/Write) are defined; OAuth scopes are
/// domain-specific. Consumers supply their own scope strings as parameters to <c>SetupAuthorization</c> and <c>RegisterSwagger</c>.
/// </remarks>
public static class ApiAccess
{
    public static class Policy
    {
        public const string Admin = "AdminAccess";
        public const string Read = "ReadAccess";
        public const string Write = "WriteAccess";
    }
}
