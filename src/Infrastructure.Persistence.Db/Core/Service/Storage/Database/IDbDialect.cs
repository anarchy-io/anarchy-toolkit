using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;

/// <summary>
///     Describes the provider-specific behaviour of a relational database (error classification, savepoint syntax,
///     batch parameter binding, health check and test SQL). Implemented once per provider (for example
///     <c>PostgresDbDialect</c>) and passed as a generic type argument to the provider-agnostic bases, so no virtual
///     dispatch or constructor wiring is needed.
/// </summary>
/// <remarks>
///     <para>
///     Members marked <c>static virtual</c> carry an ANSI SQL / ADO.NET default; providers only override what differs.
///     </para>
///     <para>
///     Consumers of the dialect include the repository bases (for example
///     <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbStorageRepositoryBase{TDbConnection, TDialect}" />),
///     <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbAsyncRetryPolicyBuilder{TDbConnection, TDialect}" />
///     and <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report.DbHealthReportBase{TDbConnection, TDialect}" />.
///     </para>
/// </remarks>
/// <example>
///     <code language="csharp">
///     public sealed class MyDbDialect : IDbDialect
///     {
///         public static string Name => "mydb";
///         public static bool IsNoData(Exception exception) => exception is InvalidOperationException;
///         public static bool IsUniqueViolation(Exception exception) =>
///             exception is DbException { SqlState: "23505" };
///     }
///     </code>
/// </example>
public interface IDbDialect
{
    /// <summary>Short lower-case provider name (for example <c>postgres</c>), used in observability names.</summary>
    static abstract string Name { get; }

    /// <summary>Whether the exception means the query returned no row where exactly one was expected.</summary>
    /// <param name="exception">The exception raised while executing the query.</param>
    /// <returns><see langword="true" /> when the exception signals a missing row.</returns>
    static abstract bool IsNoData(Exception exception);

    /// <summary>Whether the exception is a unique-constraint violation.</summary>
    /// <param name="exception">The exception raised while executing the statement.</param>
    /// <returns><see langword="true" /> when the exception is a unique-constraint violation.</returns>
    static abstract bool IsUniqueViolation(Exception exception);

    /// <summary>
    ///     Whether the exception was raised by the database server while executing a statement (as opposed to a
    ///     connection or client failure), meaning the transaction is still usable after rolling back to a savepoint.
    /// </summary>
    /// <param name="exception">The exception raised while executing the statement.</param>
    /// <returns>
    ///     <see langword="true" /> when it is a server error. The default returns <see langword="true" /> for any
    ///     <see cref="DbException" />.
    /// </returns>
    static virtual bool IsServerError(Exception exception) => exception is DbException;

    /// <summary>Whether the exception is transient and the operation may be retried by the resilience pipeline.</summary>
    /// <param name="exception">The exception raised by the operation.</param>
    /// <returns>
    ///     <see langword="true" /> when the operation may be retried. The default only treats
    ///     <see cref="TimeoutException" /> as transient.
    /// </returns>
    static virtual bool IsTransient(Exception exception) => exception is TimeoutException;

    /// <summary>
    ///     Whether a single query can insert a row and select it back (for example a PostgreSQL CTE with
    ///     <c>INSERT … RETURNING</c>). When <c>false</c>, get-or-insert flows run an insert-if-missing statement
    ///     followed by a lookup query on the same connection.
    /// </summary>
    static virtual bool CanSelectFromInsert => true;

    /// <summary>SQL creating a savepoint, or <c>null</c> when not supported.</summary>
    /// <param name="savepointName">Name of the savepoint.</param>
    /// <returns>The SQL statement; defaults to <c>SAVEPOINT {savepointName}</c>.</returns>
    static virtual string SavepointCreateSql(string savepointName) => $"SAVEPOINT {savepointName}";

    /// <summary>SQL rolling back to a savepoint, or <c>null</c> when not supported.</summary>
    /// <param name="savepointName">Name of the savepoint.</param>
    /// <returns>The SQL statement; defaults to <c>ROLLBACK TO SAVEPOINT {savepointName}</c>.</returns>
    static virtual string SavepointRollbackSql(string savepointName) => $"ROLLBACK TO SAVEPOINT {savepointName}";

    /// <summary>SQL releasing a savepoint, or <c>null</c> when the provider has no such statement.</summary>
    /// <param name="savepointName">Name of the savepoint.</param>
    /// <returns>
    ///     The SQL statement (defaults to <c>RELEASE SAVEPOINT {savepointName}</c>), or <see langword="null" /> to
    ///     skip the release step.
    /// </returns>
    static virtual string? SavepointReleaseSql(string savepointName) => $"RELEASE SAVEPOINT {savepointName}";

    /// <summary>
    ///     Converts the per-entity inputs of a batch save into the Dapper parameters of a single command.
    ///     The default returns the inputs list, which makes Dapper execute the command once per input.
    /// </summary>
    /// <param name="inputs">The per-entity parameter objects produced by the repository.</param>
    /// <returns>The object passed as Dapper parameters for the batch command.</returns>
    static virtual object ToBatchParameters(IReadOnlyList<object> inputs) => inputs;

    /// <summary>Lightweight query returning the current server time, used by connectivity health checks.</summary>
    static virtual string HealthCheckSql => "SELECT CURRENT_TIMESTAMP";

    /// <summary>SQL emptying a table, used by test helpers.</summary>
    /// <param name="tableName">Name of the table to empty (inserted verbatim, not escaped).</param>
    /// <returns>The SQL statement; defaults to <c>TRUNCATE TABLE {tableName}</c>.</returns>
    static virtual string TruncateTableSql(string tableName) => $"TRUNCATE TABLE {tableName}";
}
