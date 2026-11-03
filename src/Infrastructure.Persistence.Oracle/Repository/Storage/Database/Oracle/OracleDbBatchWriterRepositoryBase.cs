using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Repository.Storage.Database.Oracle;

public abstract class OracleDbBatchWriterRepositoryBase<TNewEntity>(
    IStoragePolicy<OracleConnection> dbPolicy,
    Observer observer)
    : DbBatchWriterRepositoryBase<OracleConnection, OracleDbDialect, TNewEntity>(dbPolicy, observer)
    where TNewEntity : IEntity;
