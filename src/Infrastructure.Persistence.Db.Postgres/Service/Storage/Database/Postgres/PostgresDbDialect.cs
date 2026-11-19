using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;

/// <summary>
///     PostgreSQL flavour of <see cref="IDbDialect" />.
/// </summary>
/// <remarks>
///     Batch saves bind one array parameter per column (see <see cref="ToBatchParameters" />), and get-or-insert flows
///     can select from an insert. Transient classification uses the default (only <see cref="TimeoutException" />).
/// </remarks>
public sealed class PostgresDbDialect : IDbDialect
{
    /// <inheritdoc />
    /// <value><c>postgres</c>.</value>
    public static string Name => "postgres";

    /// <inheritdoc />
    /// <remarks>Matches an <see cref="NpgsqlException" /> with SQL state <c>02000</c> (no data).</remarks>
    public static bool IsNoData(Exception exception)
    {
        return exception is NpgsqlException { SqlState: PostgresErrorCodes.NoData };
    }

    /// <inheritdoc />
    /// <remarks>Matches a <see cref="PostgresException" /> with SQL state <c>23505</c>.</remarks>
    public static bool IsUniqueViolation(Exception exception)
    {
        return exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    /// <inheritdoc />
    /// <remarks>Any <see cref="PostgresException" /> (an error reported by the server).</remarks>
    public static bool IsServerError(Exception exception)
    {
        return exception is PostgresException;
    }

    /// <summary>
    ///     Binds every input property as an array parameter (one array per column), so a single
    ///     <c>INSERT … SELECT * FROM unnest(@Column1, @Column2, …)</c> statement saves the whole batch.
    /// </summary>
    /// <param name="inputs">
    ///     The per-entity parameter objects; must be non-empty and share the same type (the first item's properties
    ///     define the columns).
    /// </param>
    /// <returns>A <see cref="DynamicParameters" /> holding one array per property.</returns>
    public static object ToBatchParameters(IReadOnlyList<object> inputs)
    {
        var unnest = new DynamicParameters();

        foreach (var inputProp in inputs[0].GetType().GetProperties())
        {
            var values = Array.CreateInstance(inputProp.PropertyType, inputs.Count);

            for (var idx = 0; idx < inputs.Count; idx++)
                values.SetValue(inputProp.GetValue(inputs[idx]), idx);

            unnest.Add(inputProp.Name, values);
        }

        return unnest;
    }

    /// <summary>SQL emptying a table, resetting its identity sequences and cascading to dependent tables.</summary>
    /// <param name="tableName">Name of the table to empty (inserted verbatim, not escaped).</param>
    /// <returns><c>TRUNCATE TABLE {tableName} RESTART IDENTITY CASCADE</c>.</returns>
    public static string TruncateTableSql(string tableName)
    {
        return $"TRUNCATE TABLE {tableName} RESTART IDENTITY CASCADE";
    }
}
