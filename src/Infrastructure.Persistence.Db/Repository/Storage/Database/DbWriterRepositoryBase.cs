using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

/// <summary>
///     Base for single-entity write repositories: implements
///     <see cref="IStorageWriterRepository{TNewEntity, TPersistedEntity}" /> by executing an embedded SQL file that
///     inserts the entity and returns the persisted row.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect.</typeparam>
/// <typeparam name="TNewEntity">The entity type accepted for saving.</typeparam>
/// <typeparam name="TPersistedEntity">The persisted entity type returned after saving.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
/// <example>
///     <code language="csharp">
///     public sealed class OrderWriterRepository(IStoragePolicy&lt;NpgsqlConnection&gt; dbPolicy, IAppTelemetry telemetry)
///         : DbWriterRepositoryBase&lt;NpgsqlConnection, PostgresDbDialect, NewOrder, PersistedOrder&gt;(dbPolicy, telemetry)
///     {
///         public override Task&lt;long&gt; GetLatestIdAsync(CancellationToken? cancellationToken = null) =>
///             GetLatestSequenceValueAsync("order_id_seq", cancellationToken);
///
///         // The query must return the inserted row, e.g. INSERT ... RETURNING *
///         public override Task&lt;PersistedOrder&gt; SaveAsync(NewOrder entity, CancellationToken? cancellationToken = null) =>
///             SaveAsync("Order.Save", entity, cancellationToken);
///     }
///     </code>
/// </example>
public abstract class DbWriterRepositoryBase<TDbConnection, TDialect, TNewEntity, TPersistedEntity>(
    IStoragePolicy<TDbConnection> dbPolicy,
    IAppTelemetry telemetry
)
    : DbStorageRepositoryBase<TDbConnection, TDialect>(dbPolicy, telemetry),
      IStorageWriterRepository<TNewEntity, TPersistedEntity>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
    where TNewEntity : IEntity
    where TPersistedEntity : IPersistedEntity
{
    /// <inheritdoc />
    public abstract Task<TPersistedEntity> SaveAsync(TNewEntity entity,
                                                     CancellationToken? cancellationToken = null);

    /// <summary>
    /// Allows derived classes to transform the input entity into a different object for saving.
    /// By default, it returns the entity itself.
    /// </summary>
    /// <param name="entity">Envelope to be transformed before saving</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The object passed as Dapper parameters to the save query.</returns>
    protected virtual Task<object> ToSaveInput(TNewEntity entity, CancellationToken? cancellationToken = null)
    {
        return Task.FromResult((object)entity);
    }

    /// <summary>Log context properties pushed while saving <paramref name="newEntity" />.</summary>
    /// <param name="newEntity">The entity being saved.</param>
    /// <returns>The properties; by default only <c>EntityType</c> with the entity type name.</returns>
    protected virtual KeyValuePair<string, object?>[] ToLogContext(TNewEntity newEntity)
    {
        return [
            new("EntityType", typeof(TNewEntity).Name)
        ];
    }

    /// <summary>
    ///     Saves <paramref name="entity" /> by executing the named query with the parameters returned by
    ///     <see cref="ToSaveInput" /> and maps the single returned row onto <typeparamref name="TPersistedEntity" />.
    /// </summary>
    /// <param name="sqlFileName">Name of the embedded SQL file; the query must return exactly one row.</param>
    /// <param name="entity">The entity to save.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted entity.</returns>
    /// <remarks>Failures are logged and rethrown unchanged.</remarks>
    protected async Task<TPersistedEntity> SaveAsync(string sqlFileName, TNewEntity entity,
                                                     CancellationToken? cancellationToken = null)
    {
        using (Telemetry.LogContext.PushProperties(ToLogContext(entity)))
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

                    Telemetry.Log(LogLevel.Information, caller, null, "Successfully saved entity");

                    return result;
                }
                catch (Exception e)
                {
                    Telemetry.Log(LogLevel.Error, caller, e, "Failed to save entity");
                    throw;
                }
            }, sqlFileName, cancellationToken);
        }
    }
}
