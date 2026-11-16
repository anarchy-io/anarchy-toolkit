namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

/// <summary>
///     Options that configure the database resilience (retry) pipeline.
/// </summary>
/// <param name="RetryCount">Maximum number of retry attempts for transient failures.</param>
/// <remarks>
///     Consumed by <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbAsyncRetryPolicyBuilder{TDbConnection, TDialect}" />,
///     which retries the exceptions the dialect classifies as transient with an exponential back-off.
/// </remarks>
public record DbResilienceOptions(int RetryCount);
