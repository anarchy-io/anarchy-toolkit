using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;
using Dapper;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Service.Storage.Database;

public static class DatabaseIntegrationGate
{
    public static bool DatabaseTestsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("ANARCHY_RUN_DATABASE_TESTS"), "true", StringComparison.OrdinalIgnoreCase);
}

public class PostgresDbUnitOfWorkIntegrationTest
{
    [Fact(Skip = "Set ANARCHY_RUN_DATABASE_TESTS=true and start PostgreSQL", SkipType = typeof(DatabaseIntegrationGate), SkipUnless = nameof(DatabaseIntegrationGate.DatabaseTestsEnabled))]
    public Task AggregateWrites_CommitAndRollBackTogether() => DatabaseTransactionScenario.RunAsync(
        () => new NpgsqlConnection("Host=localhost;Port=5432;Database=anarchy_test;Username=anarchy_test;Password=anarchy_test"),
        (pool, context) => new PostgresDbUnitOfWork(pool, context), PostgresDbDialect.IsUniqueViolation, false);

    [Fact(Skip = "Set ANARCHY_RUN_DATABASE_TESTS=true and start PostgreSQL", SkipType = typeof(DatabaseIntegrationGate), SkipUnless = nameof(DatabaseIntegrationGate.DatabaseTestsEnabled))]
    public Task OrderAndEventRepositories_CommitAndRollBackTogether() => EventStoreScenario.RunPostgresAsync();
}

public class MysqlDbUnitOfWorkIntegrationTest
{
    [Fact(Skip = "Set ANARCHY_RUN_DATABASE_TESTS=true and start MySQL", SkipType = typeof(DatabaseIntegrationGate), SkipUnless = nameof(DatabaseIntegrationGate.DatabaseTestsEnabled))]
    public Task AggregateWrites_CommitAndRollBackTogether() => DatabaseTransactionScenario.RunAsync(
        () => new MySqlConnection("Server=localhost;Port=3306;Database=anarchy_test;User ID=anarchy_test;Password=anarchy_test;Ignore Command Transaction=true"),
        (pool, context) => new MysqlDbUnitOfWork(pool, context), MysqlDbDialect.IsUniqueViolation, false);

    [Fact(Skip = "Set ANARCHY_RUN_DATABASE_TESTS=true and start MySQL", SkipType = typeof(DatabaseIntegrationGate), SkipUnless = nameof(DatabaseIntegrationGate.DatabaseTestsEnabled))]
    public Task OrderAndEventRepositories_CommitAndRollBackTogether() => EventStoreScenario.RunMysqlAsync();
}

public class OracleDbUnitOfWorkIntegrationTest
{
    [Fact(Skip = "Set ANARCHY_RUN_DATABASE_TESTS=true and start Oracle", SkipType = typeof(DatabaseIntegrationGate), SkipUnless = nameof(DatabaseIntegrationGate.DatabaseTestsEnabled))]
    public Task AggregateWrites_CommitAndRollBackTogether() => DatabaseTransactionScenario.RunAsync(
        () => new OracleConnection("User Id=anarchy_test;Password=anarchy_test;Data Source=localhost:1521/FREEPDB1"),
        (pool, context) => new OracleDbUnitOfWork(pool, context), OracleDbDialect.IsUniqueViolation, true);

    [Fact(Skip = "Set ANARCHY_RUN_DATABASE_TESTS=true and start Oracle", SkipType = typeof(DatabaseIntegrationGate), SkipUnless = nameof(DatabaseIntegrationGate.DatabaseTestsEnabled))]
    public Task OrderAndEventRepositories_CommitAndRollBackTogether() => EventStoreScenario.RunOracleAsync();
}

/// <summary>Exercises provider units of work with separate test-owned entity, event and outbox tables.</summary>
internal static class DatabaseTransactionScenario
{
    public static async Task RunAsync<TConnection>(
        Func<TConnection> connectionFactory,
        Func<IDbConnectionPool<TConnection>, IAsyncFlowContext<StorageConnectionEnvelopeBase<TConnection>>, IUnitOfWork> createUnitOfWork,
        Func<Exception, bool> isUniqueViolation,
        bool oracle) where TConnection : DbConnection
    {
        var ct = TestContext.Current.CancellationToken;
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var tables = new[] { $"it_order_{suffix}", $"it_stock_{suffix}", $"it_event_{suffix}", $"it_outbox_{suffix}" };
        var created = new List<string>();
        await using var pool = new TestConnectionPool<TConnection>(connectionFactory);
        var context = new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<TConnection>>();
        var policy = new DbPolicy<TConnection>(pool, context);
        var unitOfWork = createUnitOfWork(pool, context);
        var parameter = oracle ? ":id" : "@id";

        async Task<int> ExecuteAsync(string sql, object? args = null) => await policy.ExecuteAsync(
            async (connection, token) => await connection.DataStorageConnection.ExecuteAsync(
                new CommandDefinition(sql, args, cancellationToken: token ?? CancellationToken.None)), cancellationToken: ct);

        async Task<int> CountAsync(string table, string where = "") => await policy.ExecuteAsync(
            async (connection, token) => await connection.DataStorageConnection.QuerySingleAsync<int>(
                new CommandDefinition($"SELECT COUNT(*) FROM {table} {where}", cancellationToken: token ?? CancellationToken.None)), cancellationToken: ct);

        async Task InsertAsync(string table, int id) => await ExecuteAsync(
            $"INSERT INTO {table} (id) VALUES ({parameter})", new { id });

        async Task AggregateAsync(int id, bool fail) => await unitOfWork.RunInJoinedOrManagedSessionAsync(async token => {
            await InsertAsync(tables[0], id);
            await InsertAsync(tables[1], id);
            await InsertAsync(tables[2], id);
            await InsertAsync(tables[3], id);
            if (fail) throw new InvalidOperationException("Failure after outbox insertion");
            return id;
        }, ct);

        try
        {
            foreach (var table in tables)
            {
                // DDL is deliberately outside the unit of work: Oracle commits DDL implicitly.
                await ExecuteAsync($"CREATE TABLE {table} (id INTEGER PRIMARY KEY, published INTEGER DEFAULT 0 NOT NULL, age_days INTEGER DEFAULT 0 NOT NULL)");
                created.Add(table);
            }

            // The aggregate owns its session when called alone.
            await AggregateAsync(1, false);
            foreach (var table in tables) Assert.Equal(1, await CountAsync(table));

            await unitOfWork.RunInManagedSessionAsync(async token => {
                await unitOfWork.RunInJoinedSessionAsync(async joinedToken => {
                    await Assert.ThrowsAsync<TransactionNotOwnedException>(() => unitOfWork.CommitAsync(cancellationToken: ct));
                    return true;
                }, ct);
            }, ct);

            // The caller owns the session; an exception after all writes must roll them all back.
            await Assert.ThrowsAsync<SessionCallbackException>(() => unitOfWork.RunInManagedSessionAsync(async token => {
                await AggregateAsync(2, true);
            }, ct));
            foreach (var table in tables) Assert.Equal(1, await CountAsync(table));

            // A successful participant is still rolled back when its owner later fails.
            await Assert.ThrowsAsync<SessionCallbackException>(() => unitOfWork.RunInManagedSessionAsync(async token => {
                await AggregateAsync(3, false);
                throw new InvalidOperationException("Caller failed");
            }, ct));
            foreach (var table in tables) Assert.Equal(1, await CountAsync(table));

            // The database enforces the test application's idempotency key. A duplicate aborts the owner.
            await Assert.ThrowsAsync<SessionCallbackException>(() => unitOfWork.RunInManagedSessionAsync(async token => {
                await InsertAsync(tables[0], 4);
                await InsertAsync(tables[1], 4);
                await InsertAsync(tables[2], 4);
                await InsertAsync(tables[3], 1);
            }, ct));
            foreach (var table in tables) Assert.Equal(1, await CountAsync(table));

            // A savepoint can recover a statement-level duplicate without committing the outer transaction.
            await unitOfWork.RunInManagedSessionAsync(async token => {
                await ExecuteAsync("SAVEPOINT test_duplicate");
                await InsertAsync(tables[0], 7);
                var duplicate = await Record.ExceptionAsync(() => InsertAsync(tables[3], 1));
                Assert.NotNull(duplicate);
                Assert.True(isUniqueViolation(duplicate));
                await ExecuteAsync("ROLLBACK TO SAVEPOINT test_duplicate");
                if (!oracle) await ExecuteAsync("RELEASE SAVEPOINT test_duplicate");
                await InsertAsync(tables[2], 8);
            }, ct);
            Assert.Equal(1, await CountAsync(tables[0]));
            Assert.Equal(2, await CountAsync(tables[2]));

            // Exercise a test-owned outbox lifecycle, including a batch of entries, publication and cleanup.
            await unitOfWork.RunInManagedSessionAsync(async token => {
                await InsertAsync(tables[3], 5);
                await InsertAsync(tables[3], 6);
                await ExecuteAsync($"UPDATE {tables[3]} SET published = 1, age_days = 31 WHERE id = 5");
                await ExecuteAsync($"UPDATE {tables[3]} SET published = 1 WHERE id = 6");
            }, ct);
            Assert.Equal(1, await ExecuteAsync($"DELETE FROM {tables[3]} WHERE published = 1 AND age_days > 30"));
            Assert.Equal(1, await CountAsync(tables[3], "WHERE published = 0"));
            Assert.Equal(1, await CountAsync(tables[3], "WHERE published = 1"));
        }
        finally
        {
            // Only drop tables created by this invocation, never shared schemas or other tests' data.
            foreach (var table in created)
            {
                await policy.ExecuteAsync(async (connection, _) => await connection.DataStorageConnection.ExecuteAsync(
                    new CommandDefinition($"DROP TABLE {table}", cancellationToken: CancellationToken.None)), cancellationToken: CancellationToken.None);
            }
        }
    }

    private sealed class TestConnectionPool<TConnection>(Func<TConnection> create)
        : DbConnectionPoolBase<TConnection>(DataStorageInstanceType.Writer, "") where TConnection : DbConnection
    {
        protected override TConnection CreateConnection(string connectionString) => create();
    }
}
