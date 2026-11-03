using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Record;

public record TaskOutboxDbRecord
{
    public required long Id;
    public required string TypeAggregate;
    public required string AggregateId;
    public required string TypeTask;
    public required string? Payload;
    public required DateTimeOffset CreatedAt;
    public required DateTimeOffset? PublishedAt;

    public PersistedEvent ToDomainEntity()
    {
        return new PersistedEvent(
            Id,
            TypeAggregate,
            AggregateId,
            TypeTask,
            Payload,
            CreatedAt
        );
    }
}
