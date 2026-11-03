namespace Anarchy.Toolkit.Core.Entity;

/// <summary>
/// Marker interface for a persisted entity exposing its storage identity.
/// </summary>
public interface IPersistedEntity : IEntity
{
    public long Id { get; }
}
