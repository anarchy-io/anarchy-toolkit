using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;

namespace Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;

/// <summary>Persists an order event as part of the current transaction.</summary>
public interface IOrderEvents
{
    Task SaveAsync(Order order, CancellationToken cancellationToken);
}
