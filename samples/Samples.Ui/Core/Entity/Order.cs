using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Samples.Ui.Core.Entity;

public sealed record Order(
    string Id,
    string Status,
    DateTimeOffset CreatedAt,
    bool IsPriority,
    decimal Total,
    Customer Customer,
    IReadOnlyList<OrderLine> Lines,
    IReadOnlyList<string> Tags)
{
    public const string StatusShipped = "Shipped";
    public const string StatusCancelled = "Cancelled";

    [JsonIgnore] public bool CanBeCancelled => Status is not (StatusShipped or StatusCancelled);
}

public sealed record Customer(string Id, string Name, string Email);

public sealed record OrderLine(string Sku, string Description, int Quantity, decimal UnitPrice);
