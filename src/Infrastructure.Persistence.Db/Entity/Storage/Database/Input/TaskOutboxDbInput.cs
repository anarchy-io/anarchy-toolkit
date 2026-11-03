namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Input;

public record TaskOutboxDbInput
{
    public required long TypeAggregateId { get; init; }
    public required string AggregateId { get; init; }
    public required long TypeTaskId { get; init; }
    public required string? Payload { get; init; }
    public required string EventHash { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
