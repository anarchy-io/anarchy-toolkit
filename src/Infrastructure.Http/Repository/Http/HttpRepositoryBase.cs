using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;

namespace Anarchy.Toolkit.Infrastructure.Http.Repository.Http;

#pragma warning disable CS9113 // Parameter is unread.
public abstract class HttpRepositoryBase(Observer observer, IHttpClientFactory clientFactory)
#pragma warning restore CS9113 // Parameter is unread.
{
    protected abstract string GetClientName();

    protected Task<HttpResponseMessage> MonitoredGetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.GetAsync(requestUri, cancellationToken));
    }

    protected Task<HttpResponseMessage> MonitoredPostAsJsonAsync(string requestUri, object payload, IEnumerable<HttpHeader>? httpHeaders = null, CancellationToken cancellationToken = default)
    {
        var request = GetJsonPostRequest(requestUri, payload);
        request = AddCustomHeaders(request, httpHeaders);

        return ExecuteAsync(client => client.SendAsync(request, cancellationToken));
    }

    protected Task<HttpResponseMessage> MonitoredPostAsJsonAsync(string requestUri, HttpContent? payload, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.SendAsync(new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = payload }, cancellationToken));
    }

    protected Task<HttpResponseMessage> MonitoredPutAsJsonAsync(string requestUri, HttpContent? payload, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.SendAsync(new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = payload }, cancellationToken));
    }

    protected virtual HttpClient AcquireClient()
    {
        return clientFactory.CreateClient(GetClientName());
    }

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

    private async Task<HttpResponseMessage> ExecuteAsync(Func<HttpClient, Task<HttpResponseMessage>> func)
    {
        var client = AcquireClient();

        try
        {
            return await func(client);
        }
        finally
        {
            ReleaseClient(client);
        }
    }
}
