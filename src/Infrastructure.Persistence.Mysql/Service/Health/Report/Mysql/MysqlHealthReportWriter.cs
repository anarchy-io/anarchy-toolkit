using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report.Mysql;

/// <summary>
/// Health report that verifies connectivity to the MySQL writer instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"writer"</c> storage policy.</param>
/// <param name="observer">Observer used to log failures.</param>
public class MysqlHealthReportWriter([FromKeyedServices("writer")] IStoragePolicy<MySqlConnection> dbPolicy, Observer observer)
    : MysqlHealthReportBase(dbPolicy, observer, DataStorageInstanceType.Writer);
