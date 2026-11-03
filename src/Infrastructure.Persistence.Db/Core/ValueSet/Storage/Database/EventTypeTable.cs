namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.ValueSet.Storage.Database;

/// <summary>
/// Identifies the three auxiliary type-tables intrinsic to the outbox/event-store schema.
/// Used as the partitioning key for the type-id cache and to select the relevant
/// <c>Common.*.Get.All</c> / <c>Common.*.Get.OrInsert</c> SQL resources.
/// </summary>
public enum EventTypeTable
{
    TypeAggregate,
    TypeEvent,
    TypeTask
}
