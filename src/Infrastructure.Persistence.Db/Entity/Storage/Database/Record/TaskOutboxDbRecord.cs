using Anarchy.Toolkit.Core.Events.Envelopes;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Record;

/// <summary>
/// Row shape returned by the <c>TaskOutbox.Get.*</c> queries, mapped to a <see cref="PersistedDomainEvent"/> via
/// <see cref="ToDomainEntity"/>.
/// </summary>
public record TaskOutboxDbRecord
{
    /// <summary>Identifier of the outbox task.</summary>
    public required long Id;

    /// <summary>Name of the aggregate type the task belongs to.</summary>
    public required string TypeAggregate;

    /// <summary>Identifier of the aggregate instance the task belongs to.</summary>
    public required string AggregateId;

    /// <summary>Name of the task type.</summary>
    public required string TypeTask;

    /// <summary>JSON-serialized task payload, or <see langword="null"/>.</summary>
    public required string? Payload;

    /// <summary>Moment the task was created.</summary>
    public required DateTimeOffset CreatedAt;

    /// <summary>Moment the task was published, or <see langword="null"/> while unpublished.</summary>
    public required DateTimeOffset? PublishedAt;

    /// <summary>
    /// Maps the record to its domain representation. The task type becomes the event type and
    /// <see cref="CreatedAt"/> the event moment; <see cref="PublishedAt"/> is not carried over.
    /// </summary>
    /// <returns>A <see cref="PersistedDomainEvent"/> with the record values.</returns>
    public PersistedDomainEvent ToDomainEntity()
    {
        return new PersistedDomainEvent(
            Id,
            TypeAggregate,
            AggregateId,
            TypeTask,
            Payload,
            CreatedAt
        );
    }
}
