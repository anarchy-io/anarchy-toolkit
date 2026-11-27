using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Infrastructure.Http.Common;
using Anarchy.Toolkit.Infrastructure.Http.Extensions.System.Net.Http;

namespace Anarchy.Toolkit.Infrastructure.Http.Repository.Http;

/// <summary>
/// Base class for repositories (adapters) that call external HTTP APIs through a named <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// Derived classes provide the client name via <see cref="GetClientName"/>; the client itself is configured by an
/// <see cref="Anarchy.Toolkit.Infrastructure.Http.Configuration.Common.IHttpClientConfiguration"/>
/// registered with <see cref="IHttpClientFactory"/>.
/// </para>
/// <para>
/// By default each request creates a client from the factory (<see cref="AcquireClient"/>) and disposes it afterwards
/// (<see cref="ReleaseClient"/>). Disposing factory-created clients is safe because the underlying handlers are pooled
/// by the factory. See <see cref="ScopedHttpRepositoryBase"/> for a variant that reuses a single client.
/// </para>
/// <para>
/// The <c>Monitored*</c> helpers return the raw <see cref="HttpResponseMessage"/>; they do not check the status code.
/// Use <see cref="HttpResponseMessageExtensions"/> to validate and
/// deserialize responses.
/// </para>
/// </remarks>
/// <param name="telemetry">The application telemetry used to monitor requests.</param>
/// <param name="clientFactory">The factory used to create named clients.</param>
/// <example>
/// <code language="csharp">
/// public class CustomerRepository(AppTelemetry telemetry, IHttpClientFactory factory)
///     : HttpRepositoryBase(telemetry, factory)
/// {
///     protected override string GetClientName() =&gt; "customers-api";
///
///     public async Task&lt;CustomerApiModel?&gt; GetAsync(string id, CancellationToken ct)
///     {
///         var response = await MonitoredGetAsync($"customers/{id}", ct);
///         return await response
///             .EnsureSuccessStatusCode(code =&gt; new InvalidOperationException($"Status {code}"))
///             .ReadContentAsync&lt;CustomerApiModel&gt;(ct);
///     }
/// }
/// </code>
/// </example>
public abstract class HttpRepositoryBase(AppTelemetry telemetry, IHttpClientFactory clientFactory)
{
    /// <summary>
    /// Gets the name of the <see cref="HttpClient"/> (as registered with <see cref="IHttpClientFactory"/>) used by
    /// this repository.
    /// </summary>
    /// <returns>The named client identifier.</returns>
    protected abstract string GetClientName();

    /// <summary>
    /// Sends a <c>GET</c> request to <paramref name="requestUri"/>.
    /// </summary>
    /// <param name="requestUri">The request URI, absolute or relative to the client's base address.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The HTTP response; its status code is not validated.</returns>
    protected Task<HttpResponseMessage> MonitoredGetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.GetAsync(requestUri, cancellationToken), this.CalledFrom(), cancellationToken);
    }

    /// <summary>
    /// Sends a <c>POST</c> request whose body is <paramref name="payload"/> serialized as camel-cased JSON (see
    /// <see cref="JsonContent"/>), optionally adding custom request headers.
    /// </summary>
    /// <param name="requestUri">The request URI, absolute or relative to the client's base address.</param>
    /// <param name="payload">The object to serialize as the request body.</param>
    /// <param name="httpHeaders">Optional headers added to the request.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The HTTP response; its status code is not validated.</returns>
    protected Task<HttpResponseMessage> MonitoredPostAsJsonAsync(string requestUri, object payload, IEnumerable<HttpHeader>? httpHeaders = null, CancellationToken cancellationToken = default)
    {
        var request = GetJsonPostRequest(requestUri, payload);
        request = AddCustomHeaders(request, httpHeaders);

        return ExecuteAsync(client => client.SendAsync(request, cancellationToken), this.CalledFrom(), cancellationToken);
    }

    /// <summary>
    /// Sends a <c>POST</c> request with an already built <see cref="HttpContent"/> body.
    /// </summary>
    /// <param name="requestUri">The request URI, absolute or relative to the client's base address.</param>
    /// <param name="payload">The request body, or <see langword="null"/> for an empty body.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The HTTP response; its status code is not validated.</returns>
    protected Task<HttpResponseMessage> MonitoredPostAsJsonAsync(string requestUri, HttpContent? payload, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.SendAsync(new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = payload }, cancellationToken),
            this.CalledFrom(), cancellationToken);
    }

    /// <summary>
    /// Sends a <c>PUT</c> request with an already built <see cref="HttpContent"/> body.
    /// </summary>
    /// <param name="requestUri">The request URI, absolute or relative to the client's base address.</param>
    /// <param name="payload">The request body, or <see langword="null"/> for an empty body.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The HTTP response; its status code is not validated.</returns>
    protected Task<HttpResponseMessage> MonitoredPutAsJsonAsync(string requestUri, HttpContent? payload, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.SendAsync(new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = payload }, cancellationToken),
            this.CalledFrom(), cancellationToken);
    }

    /// <summary>
    /// Obtains the <see cref="HttpClient"/> used for a single request. By default creates a new client named
    /// <see cref="GetClientName"/> from the factory.
    /// </summary>
    /// <returns>The client to use.</returns>
    protected virtual HttpClient AcquireClient()
    {
        return clientFactory.CreateClient(GetClientName());
    }

    /// <summary>
    /// Releases the client obtained from <see cref="AcquireClient"/> once the request completes (successfully or not).
    /// By default disposes it.
    /// </summary>
    /// <param name="client">The client to release.</param>
    protected virtual void ReleaseClient(HttpClient client)
    {
        client.Dispose();
    }

    private static HttpRequestMessage GetJsonPostRequest(string requestUri, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Content = new JsonContent(payload);

        return request;
    }

    private static HttpRequestMessage AddCustomHeaders(HttpRequestMessage request, IEnumerable<HttpHeader>? httpHeaders = null)
    {
        if (httpHeaders == null) return request;

        foreach (var httpHeader in httpHeaders)
        {
            request.Headers.Add(httpHeader.Name, httpHeader.Value);
        }

        return request;
    }

    private Task<HttpResponseMessage> ExecuteAsync(Func<HttpClient, Task<HttpResponseMessage>> func, CallSite caller, CancellationToken cancellationToken) =>
        telemetry.MonitoredActionAsync(caller, async () => {
            var client = AcquireClient();

            try
            {
                return await func(client);
            }
            finally
            {
                ReleaseClient(client);
            }
        }, cancellationToken);
}
