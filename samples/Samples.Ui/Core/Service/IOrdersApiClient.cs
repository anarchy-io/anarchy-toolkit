using Anarchy.Toolkit.Samples.Ui.Core.Entity;

namespace Anarchy.Toolkit.Samples.Ui.Core.Service;

public interface IOrdersApiClient
{
    Task<IReadOnlyCollection<Order>> GetOrdersAsync(CancellationToken cancellationToken = default);

    Task CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
