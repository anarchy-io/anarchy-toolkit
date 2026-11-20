namespace Anarchy.Toolkit.Core.Events.Local;

/// <summary>
/// Marker interface for in-process events dispatched by
/// <see cref="Local.ILocalEventBus"/> to
/// <see cref="Local.ILocalEventHandler{TEvent}"/> implementations.
/// </summary>
/// <remarks>
/// Local events never leave the process; use them to decouple side effects inside a service. See
/// <see cref="LocalEventEnvelope"/> for a convenience base record.
/// </remarks>
public interface ILocalEvent;
