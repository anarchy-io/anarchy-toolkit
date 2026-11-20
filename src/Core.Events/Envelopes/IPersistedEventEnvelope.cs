using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Contract for an event envelope that has been persisted and exposes its storage identity.
/// </summary>
/// <remarks>
/// Combines <see cref="IEventEnvelope"/> with <see cref="IPersistedEntity.Id"/>. The default implementation is
/// <see cref="PersistedDomainEvent"/>.
/// </remarks>
public interface IPersistedEventEnvelope : IEventEnvelope, IPersistedEntity;
