using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report;

public abstract class MysqlHealthReportBase(IStoragePolicy<MySqlConnection> dbPolicy, Observer observer, DataStorageInstanceType instanceType)
    : DbHealthReportBase<MySqlConnection, MysqlDbDialect>(dbPolicy, observer, instanceType);
