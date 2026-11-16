using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Repository.Storage.Database.Oracle;

/// <summary>
/// Oracle flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbStorageRepositoryBase{TDbConnection, TDialect}"/>,
/// binding the connection type to <see cref="OracleConnection"/> and the dialect to <see cref="OracleDbDialect"/>.
/// </summary>
/// <remarks>
/// Derive from it for custom Oracle repositories that need embedded SQL loading, sequence lookups and savepoints but none
/// of the reader/writer contracts.
/// </remarks>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="observer">Observer used for logging and metrics.</param>
public abstract class OracleDbRepositoryBase(IStoragePolicy<OracleConnection> dbPolicy, Observer observer)
    : DbStorageRepositoryBase<OracleConnection, OracleDbDialect>(dbPolicy, observer);
