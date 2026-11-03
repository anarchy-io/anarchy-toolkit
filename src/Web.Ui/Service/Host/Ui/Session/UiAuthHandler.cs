using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Ui.Entity.Api.Output;

namespace Anarchy.Toolkit.Web.Ui.Service.Host.Ui.Session;

/// <summary>
/// Implements OAuth token exchange with the authentication service.
///
/// Unlike a service-to-service auth client (which uses pre-configured credentials), this exchanges dynamic
/// credentials provided at runtime by the user on the login page.
/// </summary>
public class UiAuthHandler(
    IHttpClientFactory httpClientFactory,
    Observer observer
)
{
    /// <summary>
    /// Name of the HttpClient registered by <c>AddToolkitUiAuthentication</c>.
    /// </summary>
    public const string HttpClientName = "ToolkitUiAuth";

    /// <summary>
    /// Exchanges user-provided credentials for an OAuth token using the client credentials flow.
    /// </summary>
    /// <param name="authServiceBaseUrl">The base URL of the authentication service</param>
    /// <param name="scope">The OAuth scope for which the token is requested; omitted when empty</param>
    /// <param name="clientId">The OAuth client ID (typically provided by the user/application)</param>
    /// <param name="clientSecret">The OAuth client secret (typically provided by the user/application)</param>
    /// <param name="cancellationToken">Token used to cancel the token exchange</param>
    /// <returns>An AuthTokenResponse containing the access token, or null if the exchange fails</returns>
    public async Task<AuthTokenResponse?> ExchangeCredentialsAsync(
        Uri authServiceBaseUrl,
        string? scope,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        var caller = this.CalledFrom();

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(authServiceBaseUrl, "connect/token"));

            var form = new Dictionary<string, string> {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            if (!string.IsNullOrWhiteSpace(scope))
            {
                form.Add("scope", scope);
            }

            request.Content = new FormUrlEncodedContent(form);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                observer.LogWarning(caller,
                    "Token exchange failed for client {ClientId} in {Environment} with status {StatusCode}",
                    clientId,
                    authServiceBaseUrl,
                    response.StatusCode);

                return null;
            }

            var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken);
            if (token == null)
            {
                observer.LogWarning(caller,
                    "Token exchange returned empty response for client {ClientId} in {Environment}",
                    clientId,
                    authServiceBaseUrl);
            }

            return token;
        }
        catch (Exception ex)
        {
            observer.LogError(ex,
                caller,
                "Token exchange failed for client {ClientId} in {Environment}",
                clientId,
                authServiceBaseUrl);
            throw;
        }
    }
}
