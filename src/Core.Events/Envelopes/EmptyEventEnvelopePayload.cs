namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// The only generic payload kept during extraction; represents an event envelope
/// carrying no meaningful payload.
/// </summary>
public class EmptyEventEnvelopePayload : IEventEnvelopePayload;
