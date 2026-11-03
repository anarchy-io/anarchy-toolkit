using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Testing.Db.Helper;

/// <summary>
///     Provider-agnostic test helpers working over any <see cref="IStoragePolicy{TStorage}" />.
/// </summary>
public static class DbTestHelper
{
    /// <summary>
    ///     Register a Dapper <see cref="DescriptionEnumTypeHandler{TEnum}" /> for the given enum type so that
    ///     its values are mapped to/from their <see cref="System.ComponentModel.DescriptionAttribute" /> string.
    ///     Consumers call this for each domain enum they need to persist.
    /// </summary>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum
    {
        SqlMapper.AddTypeHandler(new DescriptionEnumTypeHandler<TEnum>());
    }

    /// <summary>
    ///     Empty the given table with the <see cref="IDbDialect.TruncateTableSql" /> of <typeparamref name="TDialect" />.
    /// </summary>
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
