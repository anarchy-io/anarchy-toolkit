using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Repository.Storage.Database.Oracle;

public abstract class OracleDbRepositoryBase(IStoragePolicy<OracleConnection> dbPolicy, Observer observer)
    : DbStorageRepositoryBase<OracleConnection, OracleDbDialect>(dbPolicy, observer);
