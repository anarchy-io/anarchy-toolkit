using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;

/// <summary>MySQL error classification and SQL dialect.</summary>
public sealed class MysqlDbDialect : IDbDialect
{
    public const int UniqueViolationErrorNumber = 1062;
    public const int NoDataErrorNumber = 1329;
    public static string Name => "mysql";
    public static bool IsNoData(Exception exception) => exception is MySqlException { Number: NoDataErrorNumber };
    public static bool IsUniqueViolation(Exception exception) => exception is MySqlException { Number: UniqueViolationErrorNumber };
    // Deadlocks and connection failures can invalidate the transaction and its savepoints.
    public static bool IsServerError(Exception exception) =>
        exception is MySqlException { Number: > 0, IsTransient: false } mysql && mysql.Number is not (>= 2000 and < 3000);
    public static bool IsTransient(Exception exception) =>
        exception is TimeoutException or MySqlException { IsTransient: true } or MySqlException { ErrorCode: MySqlErrorCode.CommandTimeoutExpired };
    public static bool CanSelectFromInsert => false;
}
