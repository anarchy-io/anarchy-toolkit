using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;

namespace Anarchy.Toolkit.Samples.Examples.Orders.Infrastructure;

public sealed class InMemoryOrderRepository(InMemoryUnitOfWork unitOfWork) : IOrderRepository
{
    private readonly List<Order> _orders = [];

    public Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        unitOfWork.Enlist(() => _orders.Add(order));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Order>>([.. _orders]);
}

public sealed class InMemoryStockRepository(InMemoryUnitOfWork unitOfWork, IDictionary<string, int> initialStock)
    : IStockRepository
{
    private readonly Dictionary<string, int> _stock = new(initialStock);

    public Task<int> GetAvailableAsync(string sku, CancellationToken cancellationToken) =>
        Task.FromResult(_stock.GetValueOrDefault(sku));

    public Task ReserveAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        unitOfWork.Enlist(() => _stock[sku] = _stock.GetValueOrDefault(sku) - quantity);
        return Task.CompletedTask;
    }
}

// These in-memory stores demonstrate enlistment, not a durable event store or a publishing relay.
public sealed class InMemoryOrderEvents(InMemoryUnitOfWork unitOfWork) : IOrderEvents
{
    private readonly List<Order> _events = [];

    public Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        unitOfWork.Enlist(() => _events.Add(order));
        return Task.CompletedTask;
    }
}

public sealed class InMemoryOrderOutbox(InMemoryUnitOfWork unitOfWork) : IOrderOutbox
{
    private readonly List<Order> _pending = [];

    public Task EnqueueAsync(Order order, CancellationToken cancellationToken)
    {
        unitOfWork.Enlist(() => _pending.Add(order));
        return Task.CompletedTask;
    }
}
