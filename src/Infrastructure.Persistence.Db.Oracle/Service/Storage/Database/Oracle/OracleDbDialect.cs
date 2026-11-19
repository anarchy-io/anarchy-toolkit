using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;

/// <summary>
///     Oracle flavour of <see cref="IDbDialect" />.
/// </summary>
/// <remarks>
///     Batch saves use the default Dapper execute-many binding (one execution per input), and get-or-insert flows run
///     an insert-if-missing statement followed by a lookup, as Oracle cannot select from an <c>INSERT</c>.
///     Transient classification uses the default (only <see cref="TimeoutException"/>).
/// </remarks>
public sealed class OracleDbDialect : IDbDialect
{
    /// <summary>ORA-00001: unique constraint violated.</summary>
    public const int UniqueViolationErrorNumber = 1;

    /// <summary>ORA-01403: no data found.</summary>
    public const int NoDataErrorNumber = 1403;

    /// <inheritdoc/>
    /// <value><c>oracle</c>.</value>
    public static string Name => "oracle";

    /// <inheritdoc/>
    public static bool IsNoData(Exception exception)
    {
        return exception is OracleException { Number: NoDataErrorNumber };
    }

    /// <inheritdoc/>
    public static bool IsUniqueViolation(Exception exception)
    {
        return exception is OracleException { Number: UniqueViolationErrorNumber };
    }

    /// <summary>
    ///     Statement errors raised by the server. Recoverable errors (lost or broken sessions) are excluded, as the
    ///     transaction cannot be rolled back to a savepoint on such a connection.
    /// </summary>
    /// <param name="exception">The exception raised while executing the statement.</param>
    /// <returns><see langword="true"/> for non-recoverable <see cref="OracleException"/>s.</returns>
    public static bool IsServerError(Exception exception)
    {
        return exception is OracleException { IsRecoverable: false };
    }

    /// <inheritdoc/>
    /// <value><see langword="false"/>.</value>
    public static bool CanSelectFromInsert => false;

    /// <summary>Oracle releases savepoints at the end of the transaction; there is no explicit release statement.</summary>
    /// <param name="savepointName">Name of the savepoint (ignored).</param>
    /// <returns>Always <see langword="null"/>, so the release step is skipped.</returns>
    public static string? SavepointReleaseSql(string savepointName)
    {
        return null;
    }

    /// <inheritdoc/>
    /// <value><c>SELECT SYSDATE FROM DUAL</c>.</value>
    public static string HealthCheckSql => "SELECT SYSDATE FROM DUAL";
}
