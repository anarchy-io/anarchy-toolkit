namespace Anarchy.Toolkit.Core.Events.Entity.Event.InMemory;

/// <summary>
/// Marker interface for in-process events dispatched by
/// <see cref="Service.Event.InMemory.ILocalEventBus"/> to
/// <see cref="Service.Event.InMemory.ILocalEventHandler{TEvent}"/> implementations.
/// </summary>
/// <remarks>
/// Local events never leave the process; use them to decouple side effects inside a service. See
/// <see cref="LocalEventEnvelope"/> for a convenience base record.
/// </remarks>
public interface ILocalEvent;
