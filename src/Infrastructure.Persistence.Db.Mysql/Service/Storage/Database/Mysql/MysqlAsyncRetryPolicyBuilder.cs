using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;

/// <summary>
/// MySQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbAsyncRetryPolicyBuilder{TDbConnection, TDialect}"/>:
/// builds the exponential back-off retry pipeline for exceptions that <see cref="MysqlDbDialect"/> classifies as transient.
/// </summary>
/// <remarks>Registered as a singleton by <c>ModuleMysql</c>.</remarks>
/// <param name="options">Resilience options; <c>RetryCount</c> sets the maximum number of retries.</param>
/// <param name="telemetry">Application telemetry used to log a warning on every retry.</param>
public class MysqlAsyncRetryPolicyBuilder(MysqlResilienceOptions options, IAppTelemetry? telemetry = null)
    : DbAsyncRetryPolicyBuilder<MySqlConnection, MysqlDbDialect>(options, telemetry);
