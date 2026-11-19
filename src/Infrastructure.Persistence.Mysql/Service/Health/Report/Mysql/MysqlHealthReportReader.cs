using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report.Mysql;

/// <summary>
/// Health report that verifies connectivity to the MySQL reader instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"reader"</c> storage policy.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
public class MysqlHealthReportReader([FromKeyedServices("reader")] IStoragePolicy<MySqlConnection> dbPolicy, IAppTelemetry telemetry)
    : MysqlHealthReportBase(dbPolicy, telemetry, DataStorageInstanceType.Reader);
