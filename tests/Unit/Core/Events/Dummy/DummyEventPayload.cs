using Anarchy.Toolkit.Core.Events.Entity.Event;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;

public class DummyEventPayload : IEventEnvelopePayload
{
    public string Status { get; set; } = string.Empty;
    public long Number { get; set; }
    public DateTimeOffset LastModificationTime { get; set; }
}
