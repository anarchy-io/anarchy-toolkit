using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Repository.Storage.Database.Oracle;

/// <summary>
/// Oracle flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbBatchWriterRepositoryBase{TDbConnection, TDialect, TNewEntity}"/>
/// for repositories that save collections of entities.
/// </summary>
/// <typeparam name="TNewEntity">The entity type accepted for saving.</typeparam>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public abstract class OracleDbBatchWriterRepositoryBase<TNewEntity>(
    IStoragePolicy<OracleConnection> dbPolicy,
    IAppTelemetry telemetry)
    : DbBatchWriterRepositoryBase<OracleConnection, OracleDbDialect, TNewEntity>(dbPolicy, telemetry)
    where TNewEntity : IEntity;
