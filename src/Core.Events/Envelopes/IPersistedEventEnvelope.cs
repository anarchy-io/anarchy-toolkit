using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Contract for an event envelope that has been persisted and exposes its storage identity.
/// </summary>
/// <remarks>
/// Combines <see cref="IEventEnvelope"/> with <see cref="IPersistedEntity.Id"/>. The default implementation is
/// <see cref="PersistedDomainEvent"/>. The ID is assigned by storage, not by a message broker or CloudEvents producer.
/// </remarks>
public interface IPersistedEventEnvelope : IEventEnvelope, IPersistedEntity;
