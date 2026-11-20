using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

/// <summary>
///     Base for batch write repositories: implements <see cref="IStorageBatchWriterRepository{TNewEntity}" /> with two
///     strategies, a single batch command (<see cref="SaveAllAsync(string, IReadOnlyCollection{TNewEntity}, System.Nullable{System.Threading.CancellationToken})" />)
///     and a per-entity loop guarded by savepoints that collects duplicates (<see cref="SaveAllLoopAsync" />).
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect, used for batch parameter binding, savepoints and error classification.</typeparam>
/// <typeparam name="TNewEntity">The entity type accepted for saving.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public abstract class DbBatchWriterRepositoryBase<TDbConnection, TDialect, TNewEntity>(
    IStoragePolicy<TDbConnection> dbPolicy,
    AppTelemetry telemetry)
    : DbStorageRepositoryBase<TDbConnection, TDialect>(dbPolicy, telemetry),
      IStorageBatchWriterRepository<TNewEntity>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
    where TNewEntity : IEntity
{
    /// <inheritdoc />
    public abstract Task<int> SaveAllAsync(IReadOnlyCollection<TNewEntity> entities,
                                           CancellationToken? cancellationToken = null);

    /// <summary>Transforms an entity into the Dapper parameters of the save query.</summary>
    /// <param name="newEntity">The entity to save.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The parameter object for the entity.</returns>
    protected abstract Task<object> ToSaveInput(TNewEntity newEntity, CancellationToken? cancellationToken = null);

    /// <summary>Log context properties pushed while saving <paramref name="newEntity" /> in <see cref="SaveAllLoopAsync" />.</summary>
    /// <param name="newEntity">The entity being saved.</param>
    /// <returns>The properties; by default only <c>EntityType</c> with the entity type name.</returns>
    protected virtual KeyValuePair<string, object?>[] ToLogContext(TNewEntity newEntity)
    {
        return [
            new("EntityType", typeof(TNewEntity).Name)
        ];
    }

    /// <summary>
    ///     Saves the entities one by one with the named query on a single connection. Each insert is wrapped in a
    ///     savepoint, so a unique-constraint violation only rolls back that entity and the loop continues.
    /// </summary>
    /// <param name="sqlFileName">Name of the embedded SQL file executed for each entity.</param>
    /// <param name="entities">The entities to save.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of entities saved.</returns>
    /// <exception cref="DuplicatedEntityStorageException">
    ///     Thrown after the loop when one or more entities violated a unique constraint; the other entities remain saved.
    /// </exception>
    /// <remarks>
    ///     Savepoints are only issued when the connection is in a transaction (see
    ///     <see cref="DbStorageRepositoryBase{TDbConnection, TDialect}.SavePointCreateAsync" />); outside a
    ///     transaction each insert auto-commits. Server errors other than unique violations are logged and rethrown,
    ///     aborting the loop.
    /// </remarks>
    protected async Task<int> SaveAllLoopAsync(string sqlFileName, IReadOnlyCollection<TNewEntity> entities,
                                               CancellationToken? cancellationToken = null)
    {
        var caller = this.CalledFrom();
        var transactionSavepoint = $"Savepoint_Before_{GetType().Name}_SaveAllLoop";
        var duplicates = new List<IEntity>();
        var insertCount = 0;
        var sqlFile = LoadSql(sqlFileName);

        await DbPolicy.ExecuteAsync(async (connEnvelope, _) => {
            foreach (var entity in entities)
            {
                using (Telemetry.LogContext.PushProperties(ToLogContext(entity)))
                {
                    await SavePointCreateAsync(connEnvelope, transactionSavepoint, cancellationToken);

                    try
                    {
                        await connEnvelope.DataStorageConnection.ExecuteAsync(new CommandDefinition(
                            commandText: sqlFile,
                            parameters: await ToSaveInput(entity, cancellationToken),
                            cancellationToken: cancellationToken ?? CancellationToken.None));
                        await SavePointReleaseAsync(connEnvelope, transactionSavepoint, cancellationToken);
                        insertCount++;
                        Telemetry.Log(LogLevel.Information, caller, null, "Successfully saved entity");
                    }
                    catch (Exception exception) when (TDialect.IsServerError(exception))
                    {
                        await SavePointRollbackAndReleaseAsync(connEnvelope, transactionSavepoint, cancellationToken);

                        if (TDialect.IsUniqueViolation(exception))
                        {
                            duplicates.Add(entity);
                        }
                        else
                        {
                            Telemetry.Log(LogLevel.Error, caller, exception, "Failed to save entity");
                            throw;
                        }
                    }
                }
            }
            return 0;
        }, sqlFileName, cancellationToken);

        return duplicates.Count > 0
            ? throw new DuplicatedEntityStorageException(duplicates)
            : insertCount;
    }

    /// <summary>
    ///     Saves all entities with a single execution of the named query, using the parameters produced by
    ///     <see cref="IDbDialect.ToBatchParameters" /> from each entity's <see cref="ToSaveInput" />.
    /// </summary>
    /// <param name="sqlFileName">Name of the embedded SQL file.</param>
    /// <param name="entities">The entities to save.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of affected rows reported by the database.</returns>
    /// <exception cref="EmptyEntityListForBatchSaveStorageException">Thrown when <paramref name="entities" /> is empty.</exception>
    /// <exception cref="EmptyInputListForBatchSaveStorageException">Thrown when no save input was produced.</exception>
    protected async Task<int> SaveAllAsync(string sqlFileName, IReadOnlyCollection<TNewEntity> entities,
                                           CancellationToken? cancellationToken = null)
    {
        if (entities.Count < 1) throw new EmptyEntityListForBatchSaveStorageException();

        var inputs = new List<object>();

        foreach (var entity in entities)
        {
            inputs.Add(await ToSaveInput(entity, cancellationToken));
        }

        if (inputs.Count < 1) throw new EmptyInputListForBatchSaveStorageException();

        var parameters = TDialect.ToBatchParameters(inputs);

        return await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) => await connEnvelope.DataStorageConnection.ExecuteAsync(new CommandDefinition(
                commandText: LoadSql(sqlFileName),
                parameters: parameters,
                cancellationToken: cancellationToken ?? CancellationToken.None)),
            sqlFileName,
            cancellationToken);
    }
}
