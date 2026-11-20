using Anarchy.Toolkit.Samples.Examples.Orders;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;
using Anarchy.Toolkit.Samples.Examples.Orders.Services.Aggregates;
using Anarchy.Toolkit.Samples.Examples.Orders.UseCases;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Samples.Testing.Orders;

/// <summary>
/// Tests a UseCase and its Aggregate: the UseCase owns the transaction, the aggregate joins it, and a failure leaves
/// nothing written.
/// </summary>
public class PlaceOrderTests : TestCaseBase
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Place_WithStock_SavesTheOrderAndReservesTheStock()
    {
        await using var provider = OrdersExample.BuildProvider(FixtureTelemetry.Telemetry);

        var order = await provider.GetRequiredService<PlaceOrderUseCase>().ExecuteAsync(new PlaceOrderInput("ABC-1", 3), Ct);

        (await provider.GetRequiredService<IOrderRepository>().ListAsync(Ct)).Should().ContainSingle().Which.Should().Be(order);
        (await provider.GetRequiredService<IStockRepository>().GetAvailableAsync("ABC-1", Ct)).Should().Be(2);
    }

    [Fact]
    public async Task Place_WithoutEnoughStock_RollsBackAndCountsTheFailure()
    {
        await using var provider = OrdersExample.BuildProvider(FixtureTelemetry.Telemetry);
        var useCase = provider.GetRequiredService<PlaceOrderUseCase>();

        var act = () => useCase.ExecuteAsync(new PlaceOrderInput("ABC-1", 6), Ct);

        await act.Should().ThrowAsync<OutOfStockException>();
        (await provider.GetRequiredService<IOrderRepository>().ListAsync(Ct)).Should().BeEmpty();
        (await provider.GetRequiredService<IStockRepository>().GetAvailableAsync("ABC-1", Ct)).Should().Be(5);
        FixtureTelemetry.Metrics.Metrics.Should().Contain(metric => metric.Name.EndsWith(".out_of_stock"));
    }

    [Fact]
    public async Task Aggregate_CalledAlone_OwnsItsTransaction()
    {
        await using var provider = OrdersExample.BuildProvider(FixtureTelemetry.Telemetry);

        await provider.GetRequiredService<OrderAggregate>().PlaceAsync(new PlaceOrderInput("ABC-1", 1), Ct);

        (await provider.GetRequiredService<IOrderRepository>().ListAsync(Ct)).Should().ContainSingle();
    }
}
