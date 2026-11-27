using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.AspNetCore.Authorization;

/// <summary>
/// Extension methods for <see cref="AuthorizationPolicyBuilder"/> that express OAuth scope requirements.
/// </summary>
public static class AuthorizationPolicyBuilderExtensions
{
    private static readonly string[] ScopeClaimTypes = ["scope", "scp"];

    /// <summary>
    /// Requires the user to hold at least one of <paramref name="allowedScopes"/>.
    /// </summary>
    /// <remarks>
    /// Scopes are read from the <c>scope</c> and <c>scp</c> claims, either as several claims or as one
    /// space-separated claim value. Matching is case-sensitive.
    /// </remarks>
    /// <param name="builder">The policy builder.</param>
    /// <param name="allowedScopes">The scopes that satisfy the requirement.</param>
    /// <returns>The policy builder, for chaining.</returns>
    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, params string[] allowedScopes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(allowedScopes);

        return builder.RequireAssertion(context => HasAnyScope(context.User, allowedScopes));
    }

    private static bool HasAnyScope(ClaimsPrincipal user, string[] allowedScopes)
    {
        return user.Claims
            .Where(claim => ScopeClaimTypes.Contains(claim.Type, StringComparer.Ordinal))
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Any(scope => allowedScopes.Contains(scope, StringComparer.Ordinal));
    }
}
