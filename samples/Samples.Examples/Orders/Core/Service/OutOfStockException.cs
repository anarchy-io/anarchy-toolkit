namespace Anarchy.Toolkit.Samples.Examples.Orders.Core.Service;

public sealed class OutOfStockException(string sku, int requested, int available)
    : InvalidOperationException($"Only {available} of {sku} available, {requested} requested.");
