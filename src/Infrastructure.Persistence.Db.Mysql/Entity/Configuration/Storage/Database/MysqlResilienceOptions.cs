using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;

/// <summary>Maximum number of retries for transient MySQL failures.</summary>
/// <param name="RetryCount">Maximum number of retry attempts for transient failures.</param>
/// <remarks>
/// Consumed by <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql.MysqlAsyncRetryPolicyBuilder"/>;
/// the exceptions retried are those classified as transient by
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql.MysqlDbDialect"/>.
/// </remarks>
public sealed record MysqlResilienceOptions(int RetryCount) : DbResilienceOptions(RetryCount);
