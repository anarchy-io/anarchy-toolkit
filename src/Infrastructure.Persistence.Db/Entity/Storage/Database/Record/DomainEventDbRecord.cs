using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Record;

public record DomainEventDbRecord
{
    public required long Id;
    public required string TypeAggregate;
    public required string AggregateId;
    public required string TypeEvent;
    public required string? Payload;
    public required DateTimeOffset? DateTime;

    public PersistedEvent ToDomainEntity()
    {
        return new PersistedEvent(
            Id,
            TypeAggregate,
            AggregateId,
            TypeEvent,
            Payload,
            DateTime
        );
    }
}
