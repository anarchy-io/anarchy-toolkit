using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;

/// <summary>Maximum number of retries for transient MySQL failures.</summary>
public sealed record MysqlResilienceOptions(int RetryCount) : DbResilienceOptions(RetryCount);
