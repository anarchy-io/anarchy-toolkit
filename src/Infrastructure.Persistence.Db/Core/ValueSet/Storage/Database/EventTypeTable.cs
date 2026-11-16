namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.ValueSet.Storage.Database;

/// <summary>
/// Identifies the three auxiliary type-tables intrinsic to the outbox/event-store schema.
/// Used as the partitioning key for the type-id cache and to select the relevant
/// <c>Common.*.Get.All</c> / <c>Common.*.Get.OrInsert</c> SQL resources.
/// </summary>
public enum EventTypeTable
{
    /// <summary>Table holding the aggregate type names (resources <c>Common.TypeAggregate.*</c>).</summary>
    TypeAggregate,

    /// <summary>Table holding the domain event type names (resources <c>Common.TypeEvent.*</c>).</summary>
    TypeEvent,

    /// <summary>Table holding the outbox task type names (resources <c>Common.TypeTask.*</c>).</summary>
    TypeTask
}
