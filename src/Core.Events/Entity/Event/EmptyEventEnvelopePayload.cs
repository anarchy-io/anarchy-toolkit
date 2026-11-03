namespace Anarchy.Toolkit.Core.Events.Entity.Event;

/// <summary>
/// The only generic payload kept during extraction; represents an event envelope
/// carrying no meaningful payload.
/// </summary>
public class EmptyEventEnvelopePayload : IEventEnvelopePayload;
