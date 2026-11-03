using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Entity.Event;

/// <summary>
/// Contract for an event envelope that has been persisted and exposes its storage identity.
/// </summary>
public interface IPersistedEventEnvelope : IEventEnvelope, IPersistedEntity;
