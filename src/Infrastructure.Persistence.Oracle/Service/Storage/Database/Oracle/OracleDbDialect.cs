using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;

/// <summary>
///     Oracle flavour of <see cref="IDbDialect" />.
/// </summary>
/// <remarks>
///     Batch saves use the default Dapper execute-many binding (one execution per input), and get-or-insert flows run
///     an insert-if-missing statement followed by a lookup, as Oracle cannot select from an <c>INSERT</c>.
/// </remarks>
public sealed class OracleDbDialect : IDbDialect
{
    /// <summary>ORA-00001: unique constraint violated.</summary>
    public const int UniqueViolationErrorNumber = 1;

    /// <summary>ORA-01403: no data found.</summary>
    public const int NoDataErrorNumber = 1403;

    public static string Name => "oracle";

    public static bool IsNoData(Exception exception)
    {
        return exception is OracleException { Number: NoDataErrorNumber };
    }

    public static bool IsUniqueViolation(Exception exception)
    {
        return exception is OracleException { Number: UniqueViolationErrorNumber };
    }

    /// <summary>
    ///     Statement errors raised by the server. Recoverable errors (lost or broken sessions) are excluded, as the
    ///     transaction cannot be rolled back to a savepoint on such a connection.
    /// </summary>
    public static bool IsServerError(Exception exception)
    {
        return exception is OracleException { IsRecoverable: false };
    }

    public static bool CanSelectFromInsert => false;

    /// <summary>Oracle releases savepoints at the end of the transaction; there is no explicit release statement.</summary>
    public static string? SavepointReleaseSql(string savepointName)
    {
        return null;
    }

    public static string HealthCheckSql => "SELECT SYSDATE FROM DUAL";
}
