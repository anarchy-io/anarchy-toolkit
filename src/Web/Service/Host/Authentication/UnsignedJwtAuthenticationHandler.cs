using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Anarchy.Toolkit.Web.Service.Host.Authentication;

/// <summary>
/// Custom authentication handler for development/testing environments that accepts unsigned JWT tokens.
/// This handler:
/// - Extracts and validates claims from unsigned JWT tokens
/// - Creates a valid principal even without cryptographic signature validation
/// - Allows authorization policies to perform scope-based authorization
///
/// This is specifically designed for development environments and should NEVER be used in production.
/// </summary>
internal class UnsignedJwtAuthenticationHandler(
    IOptionsMonitor<JwtBearerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    Observer observer)
    : AuthenticationHandler<JwtBearerOptions>(options, logger, encoder)
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            // Try to parse the token without validation
            var principal = ValidateTokenAndBuildPrincipal(token);

            if (principal == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Failed to build principal from token"));
            }

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            observer.LogDebug(this.CalledFrom(), "Token validation failed: {ErrorMessage}", ex.Message);
            return Task.FromResult(AuthenticateResult.Fail(ex));
        }
    }

    private ClaimsPrincipal? ValidateTokenAndBuildPrincipal(string token)
    {
        var caller = this.CalledFrom();

        try
        {
            // In local/dev mode we intentionally skip signature validation.
            // Using ReadJwtToken (no validation) instead of ValidateToken avoids
            // exception type mismatch: newer IdentityModel versions throw
            // SecurityTokenInvalidAlgorithmException for alg:none tokens and
            // SecurityTokenSignatureKeyNotFoundException for RS256 tokens with no
            // matching key - neither was caught in the previous fallback, causing
            // every authenticated request to fail with 401.
            if (!_tokenHandler.CanReadToken(token))
            {
                return null;
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwtToken.Claims, Scheme.Name);
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            observer.LogError(ex, caller, "Token parsing failed");
            throw;
        }
    }
}
