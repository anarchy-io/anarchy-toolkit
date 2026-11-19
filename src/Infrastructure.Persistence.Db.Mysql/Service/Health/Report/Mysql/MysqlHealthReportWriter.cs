using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Health.Report.Mysql;

/// <summary>
/// Health report that verifies connectivity to the MySQL writer instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"writer"</c> storage policy.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
public class MysqlHealthReportWriter([FromKeyedServices("writer")] IStoragePolicy<MySqlConnection> dbPolicy, IAppTelemetry telemetry)
    : MysqlHealthReportBase(dbPolicy, telemetry, DataStorageInstanceType.Writer);
