using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;

/// <summary>
///     Describes the provider-specific behaviour of a relational database (error classification, savepoint syntax,
///     batch parameter binding, health check and test SQL). Implemented once per provider (for example
///     <c>PostgresDbDialect</c>) and passed as a generic type argument to the provider-agnostic bases, so no virtual
///     dispatch or constructor wiring is needed.
/// </summary>
/// <remarks>
///     Members marked <c>static virtual</c> carry an ANSI SQL / ADO.NET default; providers only override what differs.
/// </remarks>
public interface IDbDialect
{
    /// <summary>Short lower-case provider name (for example <c>postgres</c>), used in observability names.</summary>
    static abstract string Name { get; }

    /// <summary>Whether the exception means the query returned no row where exactly one was expected.</summary>
    static abstract bool IsNoData(Exception exception);

    /// <summary>Whether the exception is a unique-constraint violation.</summary>
    static abstract bool IsUniqueViolation(Exception exception);

    /// <summary>
    ///     Whether the exception was raised by the database server while executing a statement (as opposed to a
    ///     connection or client failure), meaning the transaction is still usable after rolling back to a savepoint.
    /// </summary>
    static virtual bool IsServerError(Exception exception) => exception is DbException;

    /// <summary>Whether the exception is transient and the operation may be retried by the resilience pipeline.</summary>
    static virtual bool IsTransient(Exception exception) => exception is TimeoutException;

    /// <summary>
    ///     Whether a single query can insert a row and select it back (for example a PostgreSQL CTE with
    ///     <c>INSERT … RETURNING</c>). When <c>false</c>, get-or-insert flows run an insert-if-missing statement
    ///     followed by a lookup query on the same connection.
    /// </summary>
    static virtual bool CanSelectFromInsert => true;

    /// <summary>SQL creating a savepoint, or <c>null</c> when not supported.</summary>
    static virtual string SavepointCreateSql(string savepointName) => $"SAVEPOINT {savepointName}";

    /// <summary>SQL rolling back to a savepoint, or <c>null</c> when not supported.</summary>
    static virtual string SavepointRollbackSql(string savepointName) => $"ROLLBACK TO SAVEPOINT {savepointName}";

    /// <summary>SQL releasing a savepoint, or <c>null</c> when the provider has no such statement.</summary>
    static virtual string? SavepointReleaseSql(string savepointName) => $"RELEASE SAVEPOINT {savepointName}";

    /// <summary>
    ///     Converts the per-entity inputs of a batch save into the Dapper parameters of a single command.
    ///     The default returns the inputs list, which makes Dapper execute the command once per input.
    /// </summary>
    static virtual object ToBatchParameters(IReadOnlyList<object> inputs) => inputs;

    /// <summary>Lightweight query returning the current server time, used by connectivity health checks.</summary>
    static virtual string HealthCheckSql => "SELECT CURRENT_TIMESTAMP";

    /// <summary>SQL emptying a table, used by test helpers.</summary>
    static virtual string TruncateTableSql(string tableName) => $"TRUNCATE TABLE {tableName}";
}
