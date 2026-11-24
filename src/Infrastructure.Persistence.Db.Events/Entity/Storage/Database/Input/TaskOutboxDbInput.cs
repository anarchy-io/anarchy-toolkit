namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Entity.Storage.Database.Input;

/// <summary>
/// Dapper parameters for inserting a row into an application-defined task outbox.
/// </summary>
public record TaskOutboxDbInput
{
    /// <summary>Identifier of the aggregate type in the aggregate type table.</summary>
    public required long TypeAggregateId { get; init; }

    /// <summary>Identifier of the aggregate instance the task belongs to.</summary>
    public required string AggregateId { get; init; }

    /// <summary>Identifier of the task type in the task type table.</summary>
    public required long TypeTaskId { get; init; }

    /// <summary>JSON-serialized task payload, or <see langword="null"/> when there is none.</summary>
    public required string? Payload { get; init; }

    /// <summary>JSON object containing the envelope's custom attributes; <c>{}</c> when empty.</summary>
    public required string CustomAttributes { get; init; }

    /// <summary>SHA-1 hash of the aggregate type, aggregate id, payload and task type, identifying duplicate tasks.</summary>
    public required string EventHash { get; init; }

    /// <summary>Moment the task was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
