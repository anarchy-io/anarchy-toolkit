using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Health.Report;

/// <summary>
///     Base health report that verifies connectivity to an Oracle instance by executing
///     <c>SELECT SYSDATE FROM DUAL</c> over the supplied storage policy.
/// </summary>
/// <param name="dbPolicy">Policy used to execute the health check query; should target the instance being checked.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
/// <param name="dbInstanceType">The instance type (reader or writer) being checked, used in the test name.</param>
/// <remarks>
/// See <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report.DbHealthReportBase{TDbConnection, TDialect}"/> for the test naming and
/// failure handling. Use <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Health.Report.Oracle.OracleHealthReportReader"/> and
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Health.Report.Oracle.OracleHealthReportWriter"/> for the
/// standard reader/writer checks.
/// </remarks>
public abstract class OracleHealthReportBase(
    IStoragePolicy<OracleConnection> dbPolicy,
    AppTelemetry telemetry,
    DataStorageInstanceType dbInstanceType)
    : DbHealthReportBase<OracleConnection, OracleDbDialect>(dbPolicy, telemetry, dbInstanceType);
