using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.InMemory;

public record LocalEventEnvelope : ILocalEvent
{
    public DateTimeOffset EventMoment = DateHelper.Now();
    public string? EventSource = null;
}
