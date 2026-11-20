using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report;

/// <summary>
///     Base health report that verifies connectivity to a PostgreSQL instance by executing
///     <c>SELECT CURRENT_TIMESTAMP</c> over the supplied storage policy.
/// </summary>
/// <param name="dbPolicy">Policy used to execute the health check query; should target the instance being checked.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
/// <param name="dbInstanceType">The instance type (reader or writer) being checked, used in the test name.</param>
/// <remarks>
/// See <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report.DbHealthReportBase{TDbConnection, TDialect}"/> for the test naming and
/// failure handling. Use <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report.Postgres.PostgresHealthReportReader"/> and
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report.Postgres.PostgresHealthReportWriter"/> for the
/// standard reader/writer checks.
/// </remarks>
public abstract class PostgresHealthReportBase(
    IStoragePolicy<NpgsqlConnection> dbPolicy,
    AppTelemetry telemetry,
    DataStorageInstanceType dbInstanceType)
    : DbHealthReportBase<NpgsqlConnection, PostgresDbDialect>(dbPolicy, telemetry, dbInstanceType);
