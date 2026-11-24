using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;

namespace Anarchy.Toolkit.Samples.Examples.Orders.Services.Aggregates;

/// <summary>
/// Keeps an order and its stock reservation consistent: either both are written or neither. It is atomic on its own
/// but joins the transaction of a caller that already owns one (a UseCase), so it never commits what it does not own.
/// </summary>
public sealed class OrderAggregate(
    IUnitOfWork unitOfWork,
    IOrderRepository orders,
    IStockRepository stock,
    IOrderEvents events,
    IOrderOutbox outbox,
    AppTelemetry telemetry)
{
    public Task<Order> PlaceAsync(PlaceOrderInput input, CancellationToken cancellationToken = default) =>
        unitOfWork.RunInJoinedOrManagedSessionAsync(async token => {
            var ct = token ?? CancellationToken.None;
            var available = await stock.GetAvailableAsync(input.Sku, ct);
            if (available < input.Quantity)
            {
                telemetry.IncrementCounter(this.CalledFrom(), "out_of_stock");
                throw new OutOfStockException(input.Sku, input.Quantity, available);
            }

            var order = new Order(Guid.NewGuid().ToString("N"), input.Sku, input.Quantity);
            await stock.ReserveAsync(input.Sku, input.Quantity, ct);
            await orders.SaveAsync(order, ct);
            await events.SaveAsync(order, ct);
            await outbox.EnqueueAsync(order, ct);
            return order;
        }, cancellationToken);
}
