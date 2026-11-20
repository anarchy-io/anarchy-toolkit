using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui.Session;
using Anarchy.Toolkit.Web.Ui.Infrastructure.Http.Client.Exceptions;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Anarchy.Toolkit.Web.Ui.Infrastructure.Http.Client;

/// <summary>
/// Base class for UI clients of downstream APIs. Calls are authenticated with the access token of the signed-in
/// UI session and routed to the API base URL of the environment selected at login.
/// </summary>
/// <remarks>
/// Requires <c>AddToolkitUiApiHttpClient</c> (named HTTP client) and <c>AddToolkitUiAuthentication</c> (session
/// context and environment catalog). Every call is monitored through <see cref="AppTelemetry"/>; unsuccessful
/// responses throw <see cref="DownstreamHttpRequestException"/>, which <c>NotificationService.Error(Exception)</c>
/// can display with the response body.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public class OrdersClient(
///     IHttpClientFactory httpClientFactory,
///     IUiSessionContext sessionContext,
///     IUiEnvironmentCatalog environmentCatalog,
///     AppTelemetry telemetry) : UiApiClientBase(httpClientFactory, sessionContext, environmentCatalog, telemetry)
/// {
///     public async Task&lt;JsonElement&gt; GetOrderAsync(long id, CancellationToken cancellationToken = default)
///     {
///         var baseUri = await GetApiBaseUriAsync("orders");
///         var route = BindRoute("orders/{id:long}", ("id", id.ToString()));
///         return await SendAsync&lt;JsonElement&gt;(HttpMethod.Get, baseUri, route, cancellationToken: cancellationToken);
///     }
/// }
/// </code>
/// </example>
/// <param name="httpClientFactory">Creates the named HTTP client (<see cref="HttpClientName"/>).</param>
/// <param name="sessionContext">Provides the access token and target environment of the signed-in session.</param>
/// <param name="environmentCatalog">Resolves the downstream API base URLs.</param>
/// <param name="telemetry">Application telemetry used for logging and monitoring.</param>
public abstract class UiApiClientBase(
    IHttpClientFactory httpClientFactory,
    IUiSessionContext sessionContext,
    IUiEnvironmentCatalog environmentCatalog,
    AppTelemetry telemetry)
{
    /// <summary>
    /// Name of the HttpClient registered by <c>AddToolkitUiApiHttpClient</c>.
    /// </summary>
    public const string ClientName = "ToolkitUiApi";

    /// <summary>Override to use a differently configured named HttpClient.</summary>
    protected virtual string HttpClientName => ClientName;

    /// <summary>
    /// Resolves the base URL of the given downstream API for the session's target environment.
    /// </summary>
    /// <param name="apiName">The API name, as configured in <c>UiEnvironment.ApiBaseUrls</c>.</param>
    /// <returns>The API base URL.</returns>
    /// <exception cref="KeyNotFoundException">The environment or the API is not configured.</exception>
    protected async Task<Uri> GetApiBaseUriAsync(string apiName)
    {
        var env = await sessionContext.GetTargetEnvironmentAsync();
        return environmentCatalog.GetByName(env)
            .GetApiBaseUrl(apiName);
    }

    /// <summary>
    /// Sends a request (optionally with a JSON body) and discards the response body.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="baseUri">The API base URL (see <see cref="GetApiBaseUriAsync"/>).</param>
    /// <param name="endpoint">The endpoint, relative to <paramref name="baseUri"/>.</param>
    /// <param name="body">Optional body, serialized as JSON.</param>
    /// <param name="accessToken">Bearer token to use; defaults to the session's access token.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A task that completes when the call succeeds.</returns>
    /// <exception cref="InvalidOperationException">No access token is available.</exception>
    /// <exception cref="DownstreamHttpRequestException">The response status code is not successful.</exception>
    protected Task SendAsync(
        HttpMethod method,
        Uri baseUri,
        string endpoint,
        object? body = null,
        string? accessToken = null,
        CancellationToken cancellationToken = default) =>
        SendCoreAsync(method, baseUri, endpoint, body, accessToken, static (_, _) => Task.FromResult(true),
                      cancellationToken);

    /// <summary>
    /// Sends a request (optionally with a JSON body) and deserializes the JSON response into <typeparamref name="TResponse"/>.
    /// Use <see cref="JsonElement"/> to get the raw JSON tree.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="method">The HTTP method.</param>
    /// <param name="baseUri">The API base URL (see <see cref="GetApiBaseUriAsync"/>).</param>
    /// <param name="endpoint">The endpoint, relative to <paramref name="baseUri"/>.</param>
    /// <param name="body">Optional body, serialized as JSON.</param>
    /// <param name="accessToken">Bearer token to use; defaults to the session's access token.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The deserialized response.</returns>
    /// <exception cref="InvalidOperationException">No access token is available, or the response body is JSON <c>null</c>.</exception>
    /// <exception cref="DownstreamHttpRequestException">The response status code is not successful.</exception>
    protected Task<TResponse> SendAsync<TResponse>(
        HttpMethod method,
        Uri baseUri,
        string endpoint,
        object? body = null,
        string? accessToken = null,
        CancellationToken cancellationToken = default) =>
        SendCoreAsync(method, baseUri, endpoint, body, accessToken, async (response, ct) =>
            await response.Content.ReadFromJsonAsync<TResponse>(ct) ??
            throw new InvalidOperationException(
                $"Downstream response for '{endpoint}' did not include a valid JSON body."),
            cancellationToken);

    /// <summary>
    /// Posts a GraphQL query (with optional variables) and returns the parsed response body
    /// (the full envelope, including the top-level "data"/"errors" nodes).
    /// </summary>
    /// <remarks>GraphQL errors returned with a successful status code are not turned into exceptions.</remarks>
    /// <param name="baseUri">The API base URL (see <see cref="GetApiBaseUriAsync"/>).</param>
    /// <param name="endpoint">The GraphQL endpoint, relative to <paramref name="baseUri"/>.</param>
    /// <param name="query">The GraphQL query document.</param>
    /// <param name="variables">Optional query variables.</param>
    /// <param name="accessToken">Bearer token to use; defaults to the session's access token.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The parsed response envelope.</returns>
    /// <exception cref="InvalidOperationException">No access token is available.</exception>
    /// <exception cref="DownstreamHttpRequestException">The response status code is not successful.</exception>
    protected Task<JsonElement> PostGraphQlAsync(
        Uri baseUri,
        string endpoint,
        string query,
        object? variables = null,
        string? accessToken = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Post, baseUri, endpoint, new { query, variables }, accessToken,
                               cancellationToken);

    private async Task<TResult> SendCoreAsync<TResult>(
        HttpMethod method,
        Uri baseUri,
        string endpoint,
        object? body,
        string? accessToken,
        Func<HttpResponseMessage, CancellationToken, Task<TResult>> readResponse,
        CancellationToken cancellationToken)
    {
        var caller = this.CalledFrom();

        return await telemetry.MonitoredActionAsync(caller, async () => {
            var token = accessToken ?? await sessionContext.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException($"No access token available for downstream call to '{endpoint}'.");
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(method, new Uri(baseUri, endpoint));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode) throw await DownstreamHttpRequestException.CreateAsync(response, cancellationToken);

            var result = await readResponse(response, cancellationToken);

            telemetry.Log(LogLevel.Information, caller, null, "Downstream API call succeeded for {Endpoint}", endpoint);

            return result;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Replaces <c>{name}</c> (and <c>{name:long}</c>) placeholders in a route template with URL-encoded values.
    /// </summary>
    /// <param name="routeTemplate">The route template.</param>
    /// <param name="parameters">The placeholder names and their (unencoded) values.</param>
    /// <returns>The bound route; unknown placeholders are left untouched.</returns>
    protected static string BindRoute(string routeTemplate, params (string name, string value)[] parameters)
    {
        var route = routeTemplate;

        foreach (var (name, value) in parameters)
        {
            var encodedValue = Uri.EscapeDataString(value);
            route = route.Replace($"{{{name}}}", encodedValue, StringComparison.Ordinal);
            route = route.Replace($"{{{name}:long}}", encodedValue, StringComparison.Ordinal);
        }

        return route;
    }
}
