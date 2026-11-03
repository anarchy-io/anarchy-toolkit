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
public abstract class OracleHealthReportBase(
    IStoragePolicy<OracleConnection> dbPolicy,
    Observer observer,
    DataStorageInstanceType dbInstanceType)
    : DbHealthReportBase<OracleConnection, OracleDbDialect>(dbPolicy, observer, dbInstanceType);
