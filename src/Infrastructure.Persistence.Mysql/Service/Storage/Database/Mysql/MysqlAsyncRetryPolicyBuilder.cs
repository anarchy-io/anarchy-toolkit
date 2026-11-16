using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;

/// <summary>
/// MySQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbAsyncRetryPolicyBuilder{TDbConnection, TDialect}"/>:
/// builds the exponential back-off retry pipeline for exceptions that <see cref="MysqlDbDialect"/> classifies as transient.
/// </summary>
/// <remarks>Registered as a singleton by <c>ModuleMysql</c>.</remarks>
/// <param name="options">Resilience options; <c>RetryCount</c> sets the maximum number of retries.</param>
/// <param name="observer">Observer used to log a warning on every retry.</param>
public class MysqlAsyncRetryPolicyBuilder(MysqlResilienceOptions options, Observer observer)
    : DbAsyncRetryPolicyBuilder<MySqlConnection, MysqlDbDialect>(options, observer);
