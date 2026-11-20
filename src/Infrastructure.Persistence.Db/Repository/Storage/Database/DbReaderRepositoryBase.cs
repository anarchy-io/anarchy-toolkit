using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

/// <summary>
///     Base for read-side repositories: implements <see cref="IStorageReaderRepository{TPersistedEntity}" /> on top of
///     embedded SQL files and provides helpers to query single entities, collections and paged lists.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect, used to classify "no data" errors.</typeparam>
/// <typeparam name="TPersistedEntity">The persisted entity type returned by the repository.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
/// <remarks>
///     Queries are resolved by name with <see cref="DbRepositoryBase{TDbConnection}.LoadSql" /> and mapped by Dapper
///     directly onto <typeparamref name="TPersistedEntity" />. Paged queries receive <c>Limit</c> and <c>Offset</c>
///     parameters built by <see cref="QueryLimiterParams" />, capped at <see cref="GetAllMaxLimit" /> rows.
/// </remarks>
/// <example>
///     <code language="csharp">
///     public sealed class OrderReaderRepository(IStoragePolicy&lt;NpgsqlConnection&gt; dbPolicy, AppTelemetry telemetry)
///         : DbReaderRepositoryBase&lt;NpgsqlConnection, PostgresDbDialect, PersistedOrder&gt;(dbPolicy, telemetry)
///     {
///         public override Task&lt;long&gt; GetLatestIdAsync(CancellationToken? cancellationToken = null) =>
///             GetLatestSequenceValueAsync("order_id_seq", cancellationToken);
///
///         // Loads the embedded resource "{namespace}.Queries.Order.Get.ById.sql"
///         public override Task&lt;PersistedOrder&gt; GetByIdAsync(long id, CancellationToken? cancellationToken = null) =>
///             GetByIdAsync("Order.Get.ById", id, cancellationToken);
///
///         public override Task&lt;IReadOnlyCollection&lt;PersistedOrder&gt;&gt; GetAllAsync(
///             int? limit = null, int? offset = null, CancellationToken? cancellationToken = null) =>
///             GetAllAsync("Order.Get.All", limit, offset, cancellationToken);
///     }
///     </code>
/// </example>
public abstract class DbReaderRepositoryBase<TDbConnection, TDialect, TPersistedEntity>(
    IStoragePolicy<TDbConnection> dbPolicy,
    AppTelemetry telemetry)
    : DbStorageRepositoryBase<TDbConnection, TDialect>(dbPolicy, telemetry),
      IStorageReaderRepository<TPersistedEntity>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
    where TPersistedEntity : IPersistedEntity
{
    /// <summary>Maximum number of rows returned by a paged query, regardless of the requested limit. Defaults to 500.</summary>
    protected int GetAllMaxLimit => 500;

    /// <inheritdoc />
    public abstract Task<TPersistedEntity> GetByIdAsync(long id, CancellationToken? cancellationToken = null);

    /// <inheritdoc />
    public abstract Task<IReadOnlyCollection<TPersistedEntity>> GetAllAsync(int? limit = null, int? offset = null,
                                                                            CancellationToken? cancellationToken = null);

    /// <summary>Executes the named query and returns the single matching entity.</summary>
    /// <param name="sqlFileName">Name of the embedded SQL file (see <see cref="DbRepositoryBase{TDbConnection}.LoadSql" />).</param>
    /// <param name="parameters">Dapper parameters for the query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching entity.</returns>
    /// <exception cref="EntityNotFoundException">
    ///     Thrown when no row matches, or when the dialect classifies the raised exception as "no data".
    /// </exception>
    protected async Task<TPersistedEntity> GetSingleByParamsAsync(
        string sqlFileName,
        object parameters,
        CancellationToken? cancellationToken = null)
    {
        TPersistedEntity? result;

        try
        {
            result = await DbPolicy.ExecuteAsync(
                async (connEnvelope, _) =>
                    await connEnvelope.DataStorageConnection.QuerySingleOrDefaultAsync<TPersistedEntity>(new CommandDefinition(
                        commandText: LoadSql(sqlFileName),
                        parameters: parameters,
                        cancellationToken: cancellationToken ?? CancellationToken.None)),
                sqlFileName,
                cancellationToken);
        }
        catch (Exception ex) when (TDialect.IsNoData(ex))
        {
            throw new EntityNotFoundException(
                $"No item of type {nameof(TPersistedEntity)} found based on filters {parameters}", ex);
        }

        return result ?? throw new EntityNotFoundException(
            $"No item of type {nameof(TPersistedEntity)} found based on filters {parameters}");
    }

    /// <summary>Executes the named query and returns the single matching entity, or a default value.</summary>
    /// <param name="sqlFileName">Name of the embedded SQL file.</param>
    /// <param name="param">Dapper parameters for the query.</param>
    /// <param name="defaultValue">Value returned when no row matches.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching entity, or <paramref name="defaultValue" />.</returns>
    protected async Task<TPersistedEntity?> GetSingleOrDefaultByParamsAsync(string sqlFileName, object param,
                                                                             TPersistedEntity? defaultValue = default,
                                                                             CancellationToken? cancellationToken = null)
    {
        return await DbPolicy.ExecuteAsync(async (connEnvelope, _) => {
            var result =
                await connEnvelope.DataStorageConnection.QuerySingleOrDefaultAsync<TPersistedEntity>(new CommandDefinition(
                    commandText: LoadSql(sqlFileName),
                    parameters: param,
                    cancellationToken: cancellationToken ?? CancellationToken.None));

            return result ?? defaultValue;
        }, sqlFileName, cancellationToken);
    }

    /// <summary>Executes the named query and returns every matching entity.</summary>
    /// <param name="sqlFileName">Name of the embedded SQL file.</param>
    /// <param name="param">Dapper parameters for the query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching entities; an empty collection when none match.</returns>
    protected async Task<IReadOnlyCollection<TPersistedEntity>> GetAllByParamsAsync(
        string sqlFileName,
        object param,
        CancellationToken? cancellationToken = null)
    {
        return await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                (await connEnvelope.DataStorageConnection.QueryAsync<TPersistedEntity>(new CommandDefinition(
                    commandText: LoadSql(sqlFileName),
                    parameters: param,
                    cancellationToken: cancellationToken ?? CancellationToken.None
                ))).ToArray(),
            sqlFileName,
            cancellationToken);
    }

    /// <summary>Executes the named query with an <c>id</c> parameter and returns the single matching entity.</summary>
    /// <param name="sqlFileName">Name of the embedded SQL file.</param>
    /// <param name="id">Identifier of the entity, bound as <c>@id</c>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when no row matches.</exception>
    protected async Task<TPersistedEntity> GetByIdAsync(string sqlFileName, long id,
                                                        CancellationToken? cancellationToken = null)
    {
        return await GetSingleByParamsAsync(sqlFileName, new { id }, cancellationToken);
    }

    /// <summary>Executes the named paged query with the <c>Limit</c> / <c>Offset</c> parameters of <see cref="QueryLimiterParams" />.</summary>
    /// <param name="sqlFileName">Name of the embedded SQL file.</param>
    /// <param name="limit">Requested page size; capped at <see cref="GetAllMaxLimit" />.</param>
    /// <param name="offset">Number of rows to skip; non-positive or <see langword="null" /> values mean zero.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The page of entities.</returns>
    protected async Task<IReadOnlyCollection<TPersistedEntity>> GetAllAsync(
        string sqlFileName, int? limit = null, int? offset = null, CancellationToken? cancellationToken = null)
    {
        return await GetAllByParamsAsync(sqlFileName, QueryLimiterParams(limit, offset), cancellationToken);
    }

    /// <summary>Builds the <c>Limit</c> / <c>Offset</c> parameters used by paged queries.</summary>
    /// <param name="limit">Requested page size; <see langword="null" /> means <see cref="GetAllMaxLimit" />, and larger values are capped to it.</param>
    /// <param name="offset">Number of rows to skip; non-positive or <see langword="null" /> values become zero.</param>
    /// <returns>An anonymous object with <c>Limit</c> and <c>Offset</c> properties.</returns>
    protected object QueryLimiterParams(int? limit = null, int? offset = null)
    {
        return new { Limit = int.Min(limit ?? int.MaxValue, GetAllMaxLimit), Offset = offset is > 0 ? offset : 0 };
    }
}
