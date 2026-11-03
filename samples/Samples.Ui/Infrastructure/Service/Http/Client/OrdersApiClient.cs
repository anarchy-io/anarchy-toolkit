using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Samples.Ui.Core.Entity;
using Anarchy.Toolkit.Samples.Ui.Core.Service;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui.Session;
using Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client;

namespace Anarchy.Toolkit.Samples.Ui.Infrastructure.Service.Http.Client;

/// <summary>
/// Calls the orders API of the environment picked at login, with the session's access token.
/// </summary>
public sealed class OrdersApiClient(
    IHttpClientFactory httpClientFactory,
    IUiSessionContext sessionContext,
    IUiEnvironmentCatalog environmentCatalog,
    Observer observer)
    : UiApiClientBase(httpClientFactory, sessionContext, environmentCatalog, observer), IOrdersApiClient
{
    /// <summary>Key of the orders API in <c>UiEnvironment.ApiBaseUrls</c>.</summary>
    public const string ApiName = "Orders";

    public async Task<IReadOnlyCollection<Order>> GetOrdersAsync(CancellationToken cancellationToken = default) =>
        await SendAsync<Order[]>(HttpMethod.Get, await GetApiBaseUriAsync(ApiName), "orders",
                                 cancellationToken: cancellationToken);

    public async Task CancelOrderAsync(string orderId, CancellationToken cancellationToken = default) =>
        await SendAsync(HttpMethod.Post, await GetApiBaseUriAsync(ApiName),
                        BindRoute("orders/{id}/cancel", ("id", orderId)), cancellationToken: cancellationToken);
}
