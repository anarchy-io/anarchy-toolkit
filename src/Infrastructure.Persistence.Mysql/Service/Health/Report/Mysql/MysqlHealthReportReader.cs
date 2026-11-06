using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report.Mysql;

public class MysqlHealthReportReader([FromKeyedServices("reader")] IStoragePolicy<MySqlConnection> dbPolicy, Observer observer)
    : MysqlHealthReportBase(dbPolicy, observer, DataStorageInstanceType.Reader);
