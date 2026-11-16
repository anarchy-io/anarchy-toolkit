using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Entity.Event;

/// <summary>
/// Contract for an event envelope that has been persisted and exposes its storage identity.
/// </summary>
/// <remarks>
/// Combines <see cref="IEventEnvelope"/> with <see cref="IPersistedEntity.Id"/>. The default implementation is
/// <see cref="Domain.PersistedEvent"/>.
/// </remarks>
public interface IPersistedEventEnvelope : IEventEnvelope, IPersistedEntity;
