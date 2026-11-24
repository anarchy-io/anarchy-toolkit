using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;

namespace Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;

/// <summary>Queues an order event for publication after the transaction commits.</summary>
public interface IOrderOutbox
{
    Task EnqueueAsync(Order order, CancellationToken cancellationToken);
}
