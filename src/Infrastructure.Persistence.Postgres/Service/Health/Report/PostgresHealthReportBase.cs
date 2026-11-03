using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Health.Report;

/// <summary>
///     Base health report that verifies connectivity to a PostgreSQL instance by executing
///     <c>SELECT CURRENT_TIMESTAMP</c> over the supplied storage policy.
/// </summary>
public abstract class PostgresHealthReportBase(
    IStoragePolicy<NpgsqlConnection> dbPolicy,
    Observer observer,
    DataStorageInstanceType dbInstanceType)
    : DbHealthReportBase<NpgsqlConnection, PostgresDbDialect>(dbPolicy, observer, dbInstanceType);
