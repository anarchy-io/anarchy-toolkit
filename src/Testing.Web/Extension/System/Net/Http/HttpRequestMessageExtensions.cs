using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Testing.Helper;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Anarchy.Toolkit.Testing.Web.Extension.System.Net.Http;

/// <summary>
/// Test extensions for <see cref="HttpRequestMessage"/> that inject a mock OAuth2 bearer
/// token so acceptance tests can exercise authorized endpoints without a real auth service.
/// </summary>
public static class HttpRequestMessageExtensions
{
    extension(HttpRequestMessage request)
    {
        /// <summary>
        /// Adds a mock unsigned JWT bearer token to the request's <c>Authorization</c> header.
        ///
        /// The token carries a <c>client_id</c> claim and one <c>scope</c> claim per supplied
        /// scope. Scopes are supplied by the caller (no domain-specific scopes are hardcoded).
        /// The token issuer defaults to the configured <c>AuthService.Host</c> from the test settings of
        /// <typeparamref name="TAppSettings"/>, and the token audience defaults to the first supplied scope.
        /// </summary>
        /// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
        /// <param name="scopes">
        /// The OAuth2 scope(s) to embed as <c>scope</c> claims. At least one scope is required.
        /// </param>
        /// <param name="clientId">The <c>client_id</c> claim value (default: <c>test-client</c>).</param>
        /// <param name="issuer">
        /// Optional token issuer. When <c>null</c>, the configured authentication service host is used.
        /// </param>
        /// <param name="audience">
        /// Optional token audience. When <c>null</c>, the first supplied scope is used.
        /// </param>
        /// <returns>The same <see cref="HttpRequestMessage"/> instance for chaining.</returns>
        public HttpRequestMessage MockAuthorizationToken<TAppSettings>(
            string[] scopes,
            string clientId = "test-client",
            string? issuer = null,
            string? audience = null)
            where TAppSettings : class, IAuthServiceAppSettings
        {
            ArgumentNullException.ThrowIfNull(scopes);
            if (scopes.Length == 0)
            {
                throw new EmptyArgumentException(nameof(scopes), "At least one scope must be supplied.");
            }

            var now = DateTime.UtcNow;
            var claims = new List<Claim> {
                new("client_id", clientId)
            };

            claims.AddRange(scopes.Select(s => new Claim("scope", s)));

            var token = new JwtSecurityToken(
                issuer: issuer ?? TestHelper.TestSettings<TAppSettings>()
                    .AuthService.Host.ToString(),
                claims: claims,
                notBefore: now,
                expires: now.AddHours(1),
                signingCredentials: null,
                audience: audience ?? scopes[0]
            );

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);

            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", $"Bearer {jwt}");

            return request;
        }

        /// <summary>
        /// Convenience overload that adds a mock bearer token for a single scope.
        /// </summary>
        /// <param name="scope">The OAuth2 scope to embed as a <c>scope</c> claim.</param>
        /// <param name="clientId">The <c>client_id</c> claim value (default: <c>test-client</c>).</param>
        /// <param name="issuer">
        /// Optional token issuer. When <c>null</c>, the configured authentication service host is used.
        /// </param>
        /// <param name="audience">
        /// Optional token audience. When <c>null</c>, the supplied scope is used.
        /// </param>
        /// <returns>The same <see cref="HttpRequestMessage"/> instance for chaining.</returns>
        /// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
        public HttpRequestMessage MockAuthorizationToken<TAppSettings>(
            string scope,
            string clientId = "test-client",
            string? issuer = null,
            string? audience = null)
            where TAppSettings : class, IAuthServiceAppSettings =>
            request.MockAuthorizationToken<TAppSettings>([scope], clientId, issuer, audience);
    }
}
