using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Entity.Configuration.Storage.Database;

/// <summary>
///     Options that configure the Oracle resilience (retry) pipeline.
/// </summary>
/// <param name="RetryCount">Maximum number of retry attempts for transient failures.</param>
/// <remarks>
/// Consumed by <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle.OracleAsyncRetryPolicyBuilder"/>;
/// the exceptions retried are those classified as transient by
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle.OracleDbDialect"/>.
/// </remarks>
public sealed record OracleResilienceOptions(int RetryCount) : DbResilienceOptions(RetryCount);
