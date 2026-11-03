using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;

/// <summary>
///     Options that configure the Oracle resilience (retry) pipeline.
/// </summary>
/// <param name="RetryCount">Maximum number of retry attempts for transient failures.</param>
public sealed record OracleResilienceOptions(int RetryCount) : DbResilienceOptions(RetryCount);
