namespace Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;

public interface IStockRepository
{
    Task<int> GetAvailableAsync(string sku, CancellationToken cancellationToken);

    Task ReserveAsync(string sku, int quantity, CancellationToken cancellationToken);
}
