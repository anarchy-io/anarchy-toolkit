using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;

/// <summary>
/// PostgreSQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbAsyncRetryPolicyBuilder{TDbConnection, TDialect}"/>:
/// builds the exponential back-off retry pipeline for exceptions that <see cref="PostgresDbDialect"/> classifies as transient.
/// </summary>
/// <remarks>Registered as a singleton by <c>ModulePostgres</c>.</remarks>
/// <param name="options">Resilience options; <c>RetryCount</c> sets the maximum number of retries.</param>
/// <param name="telemetry">Application telemetry used to log a warning on every retry.</param>
public class PostgresAsyncRetryPolicyBuilder(PostgresResilienceOptions options, IAppTelemetry? telemetry = null)
    : DbAsyncRetryPolicyBuilder<NpgsqlConnection, PostgresDbDialect>(options, telemetry);
