namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

/// <summary>
///     Options that configure the database resilience (retry) pipeline.
/// </summary>
/// <param name="RetryCount">Maximum number of retry attempts for transient failures.</param>
public record DbResilienceOptions(int RetryCount);
