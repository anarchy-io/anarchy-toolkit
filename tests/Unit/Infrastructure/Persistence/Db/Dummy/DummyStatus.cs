using System.ComponentModel;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;

/// <summary>
/// Test-local stand-in for a domain enum persisted through its <see cref="DescriptionAttribute"/>.
/// </summary>
public enum DummyStatus
{
    [Description("CREATED")] Created,
    [Description("PARTIALLY_PAID")] PartiallyPaid,
    [Description("PAID")] Paid,
    [Description("PACKED")] Packed,
    [Description("PICKED_UP_BY_CUSTOMER")] PickedUpByCustomer,
    [Description("OUT_FOR_DELIVERY")] OutForDelivery,
    [Description("DELIVERED")] Delivered,
    [Description("DELETED")] Deleted,
    [Description("UNKNOWN-HISTORICAL_DATA")] UnknownHistoricalData
}
