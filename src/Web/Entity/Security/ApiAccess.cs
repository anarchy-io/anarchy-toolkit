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
    /// <summary>
    /// Authorization policy names registered by <c>SetupAuthorization</c>; use them with
    /// <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute.Policy"/>.
    /// </summary>
    public static class Policy
    {
        /// <summary>Policy requiring the admin scope.</summary>
        public const string Admin = "AdminAccess";
        /// <summary>Policy requiring the read scope or the admin scope.</summary>
        public const string Read = "ReadAccess";
        /// <summary>Policy requiring the write scope or the admin scope.</summary>
        public const string Write = "WriteAccess";
    }
}
