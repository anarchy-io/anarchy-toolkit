using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extensions.System;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Entity.Storage.Database.Record;

/// <summary>
/// Row shape returned by the <c>DomainEvent.Get.*</c> queries, mapped to a <see cref="PersistedDomainEvent"/> via
/// <see cref="ToDomainEntity"/>.
/// </summary>
public record DomainEventDbRecord
{
    /// <summary>Identifier of the stored event.</summary>
    public required long Id;

    /// <summary>Name of the aggregate type that raised the event.</summary>
    public required string TypeAggregate;

    /// <summary>Identifier of the aggregate instance that raised the event.</summary>
    public required string AggregateId;

    /// <summary>Name of the event type.</summary>
    public required string TypeEvent;

    /// <summary>JSON-serialized event payload, or <see langword="null"/>.</summary>
    public required string? Payload;

    /// <summary>JSON object containing custom attributes, or <see langword="null"/> for older rows without attributes.</summary>
    public string? CustomAttributes;

    /// <summary>Moment the event occurred, if stored.</summary>
    public required DateTimeOffset? DateTime;

    /// <summary>Maps the record to its domain representation.</summary>
    /// <returns>A <see cref="PersistedDomainEvent"/> with the record values.</returns>
    public PersistedDomainEvent ToDomainEntity()
    {
        return new PersistedDomainEvent(
            Id,
            TypeAggregate,
            AggregateId,
            TypeEvent,
            Payload,
            DateTime,
            CustomAttributes?.FromJson<Dictionary<string, string>>()
        );
    }
}
