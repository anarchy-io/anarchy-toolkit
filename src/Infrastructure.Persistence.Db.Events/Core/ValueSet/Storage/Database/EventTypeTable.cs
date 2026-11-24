namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.ValueSet.Storage.Database;

/// <summary>
/// Identifies auxiliary type tables used by the event-store schema.
/// Used to select the SQL resources that read or register type identifiers.
/// </summary>
public enum EventTypeTable
{
    /// <summary>Table holding the aggregate type names (resources <c>Common.TypeAggregate.*</c>).</summary>
    TypeAggregate,

    /// <summary>Table holding the domain event type names (resources <c>Common.TypeEvent.*</c>).</summary>
    TypeEvent,
}
