using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Repository.Storage.Database.Oracle;

public abstract class OracleDbReaderRepositoryBase<TPersistedEntity>(
    IStoragePolicy<OracleConnection> dbPolicy,
    Observer observer)
    : DbReaderRepositoryBase<OracleConnection, OracleDbDialect, TPersistedEntity>(dbPolicy, observer)
    where TPersistedEntity : IPersistedEntity;
