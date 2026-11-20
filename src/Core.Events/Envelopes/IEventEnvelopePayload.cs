namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Marker interface for a strongly-typed payload carried by an event envelope.
/// </summary>
/// <remarks>
/// Used as the constraint of the <c>TPayload</c> parameter of <see cref="EventEnvelopeBase{TPayload}"/>. Payloads
/// are read from an envelope with <see cref="EventEnvelopeBase{TPayload}.CastPayload{TCast}"/>.
/// </remarks>
public interface IEventEnvelopePayload;
