using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;

public class DummyStateEntity : IPersistedEntity
{
    public long Id { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
