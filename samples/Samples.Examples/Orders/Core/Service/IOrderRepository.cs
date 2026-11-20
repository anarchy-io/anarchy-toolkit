using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;

namespace Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;

public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken);
}
