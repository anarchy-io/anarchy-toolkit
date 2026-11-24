using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Entity;
using Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;
using Anarchy.Toolkit.Samples.Examples.Orders.Infrastructure;
using Anarchy.Toolkit.Samples.Examples.Orders.Services.Aggregates;
using Anarchy.Toolkit.Samples.Examples.Orders.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Samples.Examples.Orders;

/// <summary>Registers the orders example and runs the UseCase: one order that fits the stock, one that does not.</summary>
public static class OrdersExample
{
    public static ServiceProvider BuildProvider(AppTelemetry? telemetry = null)
    {
        var services = new ServiceCollection();
        if (telemetry is null) services.AddCoreTelemetry();
        else services.AddSingleton(telemetry);
        services.AddSingleton<InMemoryUnitOfWork>();
        services.AddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<InMemoryUnitOfWork>());
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IOrderEvents, InMemoryOrderEvents>();
        services.AddSingleton<IOrderOutbox, InMemoryOrderOutbox>();
        services.AddSingleton<IStockRepository>(sp => new InMemoryStockRepository(
            sp.GetRequiredService<InMemoryUnitOfWork>(), new Dictionary<string, int> { ["ABC-1"] = 5 }));
        services.AddSingleton<OrderAggregate>();
        services.AddSingleton<PlaceOrderUseCase>();
        return services.BuildServiceProvider();
    }

    public static async Task RunAsync(TextWriter output)
    {
        await using var provider = BuildProvider();
        var useCase = provider.GetRequiredService<PlaceOrderUseCase>();

        var order = await useCase.ExecuteAsync(new PlaceOrderInput("ABC-1", 3));
        output.WriteLine($"Placed {order.Id}: {order.Quantity} x {order.Sku}");

        try
        {
            await useCase.ExecuteAsync(new PlaceOrderInput("ABC-1", 3));
        }
        catch (OutOfStockException exception)
        {
            output.WriteLine($"Rejected, nothing was written: {exception.Message}");
        }
    }
}
