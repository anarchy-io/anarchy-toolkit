using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;

/// <summary>MySQL error classification and SQL dialect.</summary>
/// <remarks>
/// MySQL flavour of <see cref="IDbDialect"/>. Uses the ANSI defaults for savepoints, health check SQL, truncation and
/// batch binding (one execution per input).
/// </remarks>
public sealed class MysqlDbDialect : IDbDialect
{
    /// <summary>MySQL error 1062 (<c>ER_DUP_ENTRY</c>): duplicate entry for a unique key.</summary>
    public const int UniqueViolationErrorNumber = 1062;

    /// <summary>MySQL error 1329 (<c>ER_SP_FETCH_NO_DATA</c>): no data, zero rows fetched.</summary>
    public const int NoDataErrorNumber = 1329;

    /// <inheritdoc/>
    /// <value><c>mysql</c>.</value>
    public static string Name => "mysql";

    /// <inheritdoc/>
    public static bool IsNoData(Exception exception) => exception is MySqlException { Number: NoDataErrorNumber };
    /// <inheritdoc/>
    public static bool IsUniqueViolation(Exception exception) => exception is MySqlException { Number: UniqueViolationErrorNumber };
    // Deadlocks and connection failures can invalidate the transaction and its savepoints.
    /// <summary>
    /// Non-transient server errors (positive error number). Transient errors such as deadlocks, and client/connection
    /// errors (numbers 2000-2999), are excluded since they can invalidate the transaction and its savepoints.
    /// </summary>
    /// <param name="exception">The exception raised while executing the statement.</param>
    /// <returns><see langword="true"/> when it is a recoverable server error.</returns>
    public static bool IsServerError(Exception exception) =>
        exception is MySqlException { Number: > 0, IsTransient: false } mysql && mysql.Number is not (>= 2000 and < 3000);
    /// <summary>
    /// Timeouts, MySqlConnector exceptions flagged as transient, and command timeouts.
    /// </summary>
    /// <param name="exception">The exception raised by the operation.</param>
    /// <returns><see langword="true"/> when the operation may be retried.</returns>
    public static bool IsTransient(Exception exception) =>
        exception is TimeoutException or MySqlException { IsTransient: true } or MySqlException { ErrorCode: MySqlErrorCode.CommandTimeoutExpired };
    /// <inheritdoc/>
    /// <value>
    /// <see langword="false"/>: MySQL cannot return the inserted row from the insert statement, so get-or-insert flows
    /// run an insert followed by a lookup.
    /// </value>
    public static bool CanSelectFromInsert => false;
}
