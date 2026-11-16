using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Health.Report;

/// <summary>
///     Base health report that verifies connectivity to an Oracle instance by executing
///     <c>SELECT SYSDATE FROM DUAL</c> over the supplied storage policy.
/// </summary>
/// <param name="dbPolicy">Policy used to execute the health check query; should target the instance being checked.</param>
/// <param name="observer">Observer used to log failures.</param>
/// <param name="dbInstanceType">The instance type (reader or writer) being checked, used in the test name.</param>
/// <remarks>
/// See <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report.DbHealthReportBase{TDbConnection, TDialect}"/> for the test naming and
/// failure handling. Use <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Health.Report.Oracle.OracleHealthReportReader"/> and
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Health.Report.Oracle.OracleHealthReportWriter"/> for the
/// standard reader/writer checks.
/// </remarks>
public abstract class OracleHealthReportBase(
    IStoragePolicy<OracleConnection> dbPolicy,
    Observer observer,
    DataStorageInstanceType dbInstanceType)
    : DbHealthReportBase<OracleConnection, OracleDbDialect>(dbPolicy, observer, dbInstanceType);
