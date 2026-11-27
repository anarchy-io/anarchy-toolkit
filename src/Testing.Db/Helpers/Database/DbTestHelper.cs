using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Testing.Db.Helpers.Database;

/// <summary>
///     Provider-agnostic test helpers working over any <see cref="IStoragePolicy{TStorage}" />.
/// </summary>
/// <remarks>
///     The provider test packages (<c>Testing.Db.Postgres</c>, <c>Testing.Db.Mysql</c>, <c>Testing.Db.Oracle</c>) wrap these
///     helpers with their own connection and dialect types. Table names are interpolated verbatim into SQL, so only
///     pass trusted values.
/// </remarks>
/// <example>
///     <code language="csharp">
///     DbTestHelper.RegisterEnumTypeHandler&lt;OrderStatus&gt;();
///     await DbTestHelper.TruncateTableAsync&lt;NpgsqlConnection, PostgresDbDialect&gt;(dbPolicy, "orders");
///     await DbTestHelper.ClearTableAsync(dbPolicy, "order_lines");
///     </code>
/// </example>
public static class DbTestHelper
{
    /// <summary>
    ///     Register a Dapper <see cref="DescriptionEnumTypeHandler{TEnum}" /> for the given enum type so that
    ///     its values are mapped to/from their <see cref="System.ComponentModel.DescriptionAttribute" /> string.
    ///     Consumers call this for each domain enum they need to persist.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to register.</typeparam>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum
    {
        SqlMapper.AddTypeHandler(new DescriptionEnumTypeHandler<TEnum>());
    }

    /// <summary>
    ///     Empty the given table with the <see cref="IDbDialect.TruncateTableSql" /> of <typeparamref name="TDialect" />.
    /// </summary>
    /// <typeparam name="TDbConnection">The type of database connection.</typeparam>
    /// <typeparam name="TDialect">The provider dialect supplying the truncate statement.</typeparam>
    /// <param name="dbPolicy">Policy used to execute the statement.</param>
    /// <param name="tableName">Name of the table to truncate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the table has been truncated.</returns>
    public static async Task TruncateTableAsync<TDbConnection, TDialect>(IStoragePolicy<TDbConnection> dbPolicy,
                                                                         string tableName,
                                                                         CancellationToken cancellationToken = default)
        where TDbConnection : DbConnection
        where TDialect : IDbDialect
    {
        await dbPolicy.ExecuteAsync(async (connEnvelope, _) =>
            await connEnvelope.DataStorageConnection.ExecuteAsync(new CommandDefinition(
                commandText: TDialect.TruncateTableSql(tableName),
                cancellationToken: cancellationToken)), cancellationToken: cancellationToken);
    }

    /// <summary>
    ///     Delete all rows from the given table.
    /// </summary>
    /// <typeparam name="TDbConnection">The type of database connection.</typeparam>
    /// <param name="dbPolicy">Policy used to execute the statement.</param>
    /// <param name="tableName">Name of the table to clear.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the rows have been deleted.</returns>
    /// <remarks>
    ///     Unlike <see cref="TruncateTableAsync{TDbConnection, TDialect}" />, this runs <c>DELETE FROM</c>, which works
    ///     on tables referenced by foreign keys but does not reset identity/sequence values.
    /// </remarks>
    public static async Task ClearTableAsync<TDbConnection>(IStoragePolicy<TDbConnection> dbPolicy, string tableName,
                                                            CancellationToken cancellationToken = default)
        where TDbConnection : DbConnection
    {
        await dbPolicy.ExecuteAsync(async (connEnvelope, _) =>
            await connEnvelope.DataStorageConnection.ExecuteAsync(new CommandDefinition(
                commandText: $"DELETE FROM {tableName}",
                cancellationToken: cancellationToken)), cancellationToken: cancellationToken);
    }
}
