using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report;

/// <summary>
/// Base health report that verifies connectivity to a MySQL instance by executing
/// <c>SELECT CURRENT_TIMESTAMP</c> over the supplied storage policy.
/// </summary>
/// <param name="dbPolicy">Policy used to execute the health check query; should target the instance being checked.</param>
/// <param name="observer">Observer used to log failures.</param>
/// <param name="instanceType">The instance type (reader or writer) being checked, used in the test name.</param>
/// <remarks>
/// See <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report.DbHealthReportBase{TDbConnection, TDialect}"/> for the test naming and
/// failure handling. Use <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report.Mysql.MysqlHealthReportReader"/> and
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report.Mysql.MysqlHealthReportWriter"/> for the
/// standard reader/writer checks.
/// </remarks>
public abstract class MysqlHealthReportBase(IStoragePolicy<MySqlConnection> dbPolicy, Observer observer, DataStorageInstanceType instanceType)
    : DbHealthReportBase<MySqlConnection, MysqlDbDialect>(dbPolicy, observer, instanceType);
