using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

public abstract class DbWriterRepositoryBase<TDbConnection, TDialect, TNewEntity, TPersistedEntity>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer
)
    : DbStorageRepositoryBase<TDbConnection, TDialect>(dbPolicy, observer),
      IStorageWriterRepository<TNewEntity, TPersistedEntity>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
    where TNewEntity : IEntity
    where TPersistedEntity : IPersistedEntity
{
    public abstract Task<TPersistedEntity> SaveAsync(TNewEntity entity,
                                                     CancellationToken? cancellationToken = null);

    /// <summary>
    /// Allows derived classes to transform the input entity into a different object for saving.
    /// By default, it returns the entity itself.
    /// </summary>
    /// <param name="entity">Envelope to be transformed before saving</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns></returns>
    protected virtual Task<object> ToSaveInput(TNewEntity entity, CancellationToken? cancellationToken = null)
    {
        return Task.FromResult((object)entity);
    }

    protected virtual LogContextProperty[] ToLogContext(TNewEntity newEntity)
    {
        return [
            new LogContextProperty("EntityType", typeof(TNewEntity).Name)
        ];
    }

    protected async Task<TPersistedEntity> SaveAsync(string sqlFileName, TNewEntity entity,
                                                     CancellationToken? cancellationToken = null)
    {
        using (Observer.LogContext.PushProperties(ToLogContext(entity)))
        {
            var saveInput = await ToSaveInput(entity, cancellationToken);
            var caller = this.CalledFrom();

            return await DbPolicy.ExecuteAsync(async (connEnvelope, _) => {
                try
                {
                    var result = await connEnvelope.DataStorageConnection.QuerySingleAsync<TPersistedEntity>(
                        new CommandDefinition(
                            commandText: LoadSql(sqlFileName),
                            parameters: saveInput,
                            cancellationToken: cancellationToken ?? CancellationToken.None));

                    Observer.LogInformation(caller, "Successfully saved entity");

                    return result;
                }
                catch (Exception e)
                {
                    Observer.LogError(e, caller, "Failed to save entity");
                    throw;
                }
            }, sqlFileName, cancellationToken);
        }
    }
}
