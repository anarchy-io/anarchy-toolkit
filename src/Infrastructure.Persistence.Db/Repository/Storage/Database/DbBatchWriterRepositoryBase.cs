using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

public abstract class DbBatchWriterRepositoryBase<TDbConnection, TDialect, TNewEntity>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer)
    : DbStorageRepositoryBase<TDbConnection, TDialect>(dbPolicy, observer),
      IStorageBatchWriterRepository<TNewEntity>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
    where TNewEntity : IEntity
{
    public abstract Task<int> SaveAllAsync(IReadOnlyCollection<TNewEntity> entities,
                                           CancellationToken? cancellationToken = null);

    protected abstract Task<object> ToSaveInput(TNewEntity newEntity, CancellationToken? cancellationToken = null);

    protected virtual LogContextProperty[] ToLogContext(TNewEntity newEntity)
    {
        return [
            new LogContextProperty("EntityType", typeof(TNewEntity).Name)
        ];
    }

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
                using (Observer.LogContext.PushProperties(ToLogContext(entity)))
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
                        Observer.LogInformation(caller, "Successfully saved entity");
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
                            Observer.LogError(exception, caller, "Failed to save entity");
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
