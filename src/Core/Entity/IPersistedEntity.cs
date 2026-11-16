namespace Anarchy.Toolkit.Core.Entity;

/// <summary>
/// Marker interface for a persisted entity exposing its storage identity.
/// </summary>
/// <remarks>
/// Implemented by entities that have already been stored and therefore carry the identifier assigned by the storage
/// (for example, persisted domain events in <c>Anarchy.Toolkit.Core.Events</c>).
/// </remarks>
public interface IPersistedEntity : IEntity
{
    /// <summary>
    /// Gets the storage-assigned identifier of the entity.
    /// </summary>
    public long Id { get; }
}
