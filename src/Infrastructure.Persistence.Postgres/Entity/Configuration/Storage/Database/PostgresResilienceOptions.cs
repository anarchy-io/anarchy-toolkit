using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;

/// <summary>
///     Options that configure the Postgres resilience (retry) pipeline.
/// </summary>
/// <param name="RetryCount">Maximum number of retry attempts for transient failures.</param>
/// <remarks>
/// Consumed by <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres.PostgresAsyncRetryPolicyBuilder"/>;
/// the exceptions retried are those classified as transient by
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres.PostgresDbDialect"/>.
/// </remarks>
public sealed record PostgresResilienceOptions(int RetryCount) : DbResilienceOptions(RetryCount);
