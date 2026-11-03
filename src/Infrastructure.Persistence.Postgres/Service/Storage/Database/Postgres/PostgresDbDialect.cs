using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;

/// <summary>
///     PostgreSQL flavour of <see cref="IDbDialect" />.
/// </summary>
public sealed class PostgresDbDialect : IDbDialect
{
    public static string Name => "postgres";

    public static bool IsNoData(Exception exception)
    {
        return exception is NpgsqlException { SqlState: PostgresErrorCodes.NoData };
    }

    public static bool IsUniqueViolation(Exception exception)
    {
        return exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    public static bool IsServerError(Exception exception)
    {
        return exception is PostgresException;
    }

    /// <summary>
    ///     Binds every input property as an array parameter (one array per column), so a single
    ///     <c>INSERT … SELECT * FROM unnest(@Column1, @Column2, …)</c> statement saves the whole batch.
    /// </summary>
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

    public static string TruncateTableSql(string tableName)
    {
        return $"TRUNCATE TABLE {tableName} RESTART IDENTITY CASCADE";
    }
}
