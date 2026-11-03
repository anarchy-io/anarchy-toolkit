using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui.Session;
using Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client.Exceptions;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client;

/// <summary>
/// Base class for UI clients of downstream APIs. Calls are authenticated with the access token of the signed-in
/// UI session and routed to the API base URL of the environment selected at login.
/// </summary>
public abstract class UiApiClientBase(
    IHttpClientFactory httpClientFactory,
    IUiSessionContext sessionContext,
    IUiEnvironmentCatalog environmentCatalog,
    Observer observer)
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
    protected async Task<Uri> GetApiBaseUriAsync(string apiName)
    {
        var env = await sessionContext.GetTargetEnvironmentAsync();
        return environmentCatalog.GetByName(env)
            .GetApiBaseUrl(apiName);
    }

    /// <summary>
    /// Sends a request (optionally with a JSON body) and discards the response body.
    /// </summary>
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

        return await observer.MonitoredActionAsync(caller, async () => {
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

            observer.LogInformation(caller, "Downstream API call succeeded for {Endpoint}", endpoint);

            return result;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Replaces <c>{name}</c> (and <c>{name:long}</c>) placeholders in a route template with URL-encoded values.
    /// </summary>
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
