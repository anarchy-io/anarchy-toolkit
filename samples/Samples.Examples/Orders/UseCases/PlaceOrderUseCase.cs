using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;
using Anarchy.Toolkit.Samples.Examples.Orders.Services.Aggregates;

namespace Anarchy.Toolkit.Samples.Examples.Orders.UseCases;

/// <summary>
/// The workflow a controller or handler calls: validate the request, then place the order in one transaction this
/// UseCase owns. The aggregate joins that transaction, and an exception it throws rolls everything back here.
/// </summary>
public sealed class PlaceOrderUseCase(IUnitOfWork unitOfWork, OrderAggregate aggregate, AppTelemetry telemetry)
{
    public Task<Order> ExecuteAsync(PlaceOrderInput input, CancellationToken cancellationToken = default) =>
        telemetry.MonitoredActionAsync(this.CalledFrom(), async () => {
            ArgumentOutOfRangeException.ThrowIfLessThan(input.Quantity, 1);

            var order = await unitOfWork.RunInManagedSessionAsync(
                async token => await aggregate.PlaceAsync(input, token ?? CancellationToken.None), cancellationToken);

            telemetry.LogInformation(this.CalledFrom(), "Placed order {OrderId} for {Quantity} x {Sku}",
                                     order.Id, order.Quantity, order.Sku);
            return order;
        }, cancellationToken);
}
