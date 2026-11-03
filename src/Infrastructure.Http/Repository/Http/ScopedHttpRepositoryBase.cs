using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Infrastructure.Http.Repository.Http;

public abstract class ScopedHttpRepositoryBase(
    Observer observer,
    IHttpClientFactory clientFactory,
    string clientName)
    : HttpRepositoryBase(observer, clientFactory), IDisposable
{
    private readonly HttpClient _client = clientFactory.CreateClient(clientName);

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override string GetClientName()
    {
        return clientName;
    }

    protected override HttpClient AcquireClient()
    {
        return _client;
    }

    protected override void ReleaseClient(HttpClient client)
    {
        //nothing - client is disposed at the end
    }
}
