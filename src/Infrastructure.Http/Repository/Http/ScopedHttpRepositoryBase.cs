using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Infrastructure.Http.Repository.Http;

/// <summary>
/// Variant of <see cref="HttpRepositoryBase"/> that creates a single named <see cref="HttpClient"/> up front and reuses
/// it for every request made through the repository instance.
/// </summary>
/// <remarks>
/// Intended for repositories registered with a scoped (or transient) lifetime: the client is created in the
/// constructor and disposed in <see cref="Dispose"/>, which the DI container calls when the scope ends. Do not register
/// derived types as singletons.
/// </remarks>
/// <param name="observer">The observer used for logging and metrics.</param>
/// <param name="clientFactory">The factory used to create the named client.</param>
/// <param name="clientName">The name of the <see cref="HttpClient"/> registered with <see cref="IHttpClientFactory"/>.</param>
/// <example>
/// <code language="csharp">
/// public class OrdersRepository(Observer observer, IHttpClientFactory factory)
///     : ScopedHttpRepositoryBase(observer, factory, "orders-api")
/// {
///     public Task&lt;HttpResponseMessage&gt; GetOrderAsync(string id, CancellationToken ct) =&gt;
///         MonitoredGetAsync($"orders/{id}", ct);
/// }
/// </code>
/// </example>
public abstract class ScopedHttpRepositoryBase(
    Observer observer,
    IHttpClientFactory clientFactory,
    string clientName)
    : HttpRepositoryBase(observer, clientFactory), IDisposable
{
    private readonly HttpClient _client = clientFactory.CreateClient(clientName);

    /// <summary>
    /// Disposes the <see cref="HttpClient"/> held by this repository.
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    /// <returns>The client name passed to the constructor.</returns>
    protected override string GetClientName()
    {
        return clientName;
    }

    /// <summary>
    /// Returns the client created when the repository was constructed.
    /// </summary>
    /// <returns>The shared <see cref="HttpClient"/> instance.</returns>
    protected override HttpClient AcquireClient()
    {
        return _client;
    }

    /// <summary>
    /// Does nothing; the shared client is disposed in <see cref="Dispose"/>.
    /// </summary>
    /// <param name="client">The client that was used for the request.</param>
    protected override void ReleaseClient(HttpClient client)
    {
        //nothing - client is disposed at the end
    }
}
