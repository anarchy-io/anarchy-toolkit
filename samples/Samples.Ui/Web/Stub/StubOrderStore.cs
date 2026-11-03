using Anarchy.Toolkit.Samples.Ui.Core.Entity;

namespace Anarchy.Toolkit.Samples.Ui.Web.Stub;

/// <summary>
/// In-memory orders per environment, seeded deterministically, so the stub API has something to page through.
/// </summary>
public sealed class StubOrderStore
{
    public enum CancelResult { Cancelled, NotFound, NotCancellable }

    private static readonly string[] Statuses = ["Pending", "Paid", Order.StatusShipped, Order.StatusCancelled];
    private static readonly (string Name, string Email)[] Customers = [
        ("Ada Lovelace", "ada@example.com"), ("Alan Turing", "alan@example.com"),
        ("Grace Hopper", "grace@example.com"), ("Edsger Dijkstra", "edsger@example.com"),
        ("Barbara Liskov", "barbara@example.com"), ("Donald Knuth", "donald@example.com"),
    ];
    private static readonly (string Sku, string Description, decimal Price)[] Products = [
        ("KB-001", "Mechanical keyboard", 129.00m), ("MS-002", "Wireless mouse", 49.95m),
        ("MN-003", "27\" monitor", 329.00m), ("HD-004", "Noise-cancelling headphones", 249.00m),
        ("CB-005", "USB-C cable", 14.99m), ("DK-006", "Docking station", 189.00m),
    ];

    private readonly Lock _lock = new();
    private readonly Dictionary<string, List<Order>> _ordersByEnvironment = new(StringComparer.OrdinalIgnoreCase) {
        ["development"] = Seed(seed: 1, count: 57, idPrefix: "DEV"),
        ["testing"] = Seed(seed: 2, count: 23, idPrefix: "TST"),
    };

    public IReadOnlyCollection<Order>? GetOrders(string environment)
    {
        lock (_lock)
        {
            return _ordersByEnvironment.TryGetValue(environment, out var orders) ? orders.ToArray() : null;
        }
    }

    public CancelResult Cancel(string environment, string orderId)
    {
        lock (_lock)
        {
            if (!_ordersByEnvironment.TryGetValue(environment, out var orders))
            {
                return CancelResult.NotFound;
            }

            var index = orders.FindIndex(x => x.Id == orderId);
            if (index < 0)
            {
                return CancelResult.NotFound;
            }

            if (!orders[index].CanBeCancelled)
            {
                return CancelResult.NotCancellable;
            }

            orders[index] = orders[index] with { Status = Order.StatusCancelled };
            return CancelResult.Cancelled;
        }
    }

    private static List<Order> Seed(int seed, int count, string idPrefix)
    {
        var random = new Random(seed);
        var start = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

        return Enumerable.Range(1, count)
            .Select(i => {
                var (name, email) = Customers[random.Next(Customers.Length)];
                var lines = Enumerable.Range(0, random.Next(1, 4))
                    .Select(_ => Products[random.Next(Products.Length)])
                    .Select(p => new OrderLine(p.Sku, p.Description, random.Next(1, 4), p.Price))
                    .ToArray();
                var isPriority = random.Next(5) == 0;

                return new Order(
                    Id: $"{idPrefix}-{i:D4}",
                    Status: Statuses[random.Next(Statuses.Length)],
                    CreatedAt: start.AddHours(i * 7),
                    IsPriority: isPriority,
                    Total: lines.Sum(x => x.Quantity * x.UnitPrice),
                    Customer: new Customer($"C-{Array.FindIndex(Customers, c => c.Name == name) + 1:D3}", name, email),
                    Lines: lines,
                    Tags: isPriority ? ["priority", "gift-wrap"] : ["standard"]);
            })
            .ToList();
    }
}
