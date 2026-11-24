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

[Collection("PostgreSQL database fixture")]
public class PostgresDbUnitOfWorkIntegrationTest
{
    [Fact]
    public Task AggregateWrites_CommitAndRollBackTogether() => DatabaseTransactionScenario.RunAsync(
        CreateConnection,
        (pool, context) => new PostgresDbUnitOfWork(pool, context), PostgresDbDialect.IsUniqueViolation, false);

    [Fact]
    public Task OrderAndEventRepositories_CommitAndRollBackTogether() => EventStoreScenario.RunPostgresAsync(CreateConnection);

    private static NpgsqlConnection CreateConnection() =>
        new("Host=localhost;Port=5432;Database=anarchy_test;Username=anarchy_test;Password=anarchy_test");
}

[Collection("MySQL database fixture")]
public class MysqlDbUnitOfWorkIntegrationTest
{
    [Fact]
    public Task AggregateWrites_CommitAndRollBackTogether() => DatabaseTransactionScenario.RunAsync(
        CreateConnection,
        (pool, context) => new MysqlDbUnitOfWork(pool, context), MysqlDbDialect.IsUniqueViolation, false);

    [Fact]
    public Task OrderAndEventRepositories_CommitAndRollBackTogether() => EventStoreScenario.RunMysqlAsync(CreateConnection);

    private static MySqlConnection CreateConnection() =>
        new("Server=localhost;Port=3306;Database=anarchy_test;User ID=anarchy_test;Password=anarchy_test;Ignore Command Transaction=true;DateTime Kind=Utc");
}

[Collection("Oracle database fixture")]
public class OracleDbUnitOfWorkIntegrationTest
{
    [Fact]
    public Task AggregateWrites_CommitAndRollBackTogether() => DatabaseTransactionScenario.RunAsync(
        CreateConnection,
        (pool, context) => new OracleDbUnitOfWork(pool, context), OracleDbDialect.IsUniqueViolation, true);

    [Fact]
    public Task OrderAndEventRepositories_CommitAndRollBackTogether() => EventStoreScenario.RunOracleAsync(CreateConnection);

    private static OracleConnection CreateConnection() =>
        new("User Id=anarchy_test;Password=anarchy_test;Data Source=localhost:1521/FREEPDB1");
}

/// <summary>Exercises provider units of work with the entity, event and outbox fixtures installed by Compose.</summary>
internal static class DatabaseTransactionScenario
{
    public static async Task RunAsync<TConnection>(
        Func<TConnection> connectionFactory,
        Func<IDbConnectionPool<TConnection>, IAsyncFlowContext<StorageConnectionEnvelopeBase<TConnection>>, IUnitOfWork> createUnitOfWork,
        Func<Exception, bool> isUniqueViolation,
        bool oracle) where TConnection : DbConnection
    {
        var ct = TestContext.Current.CancellationToken;
        const string orders = "it_order";
        const string stock = "it_stock";
        const string events = "it_event";
        const string outbox = "it_outbox";
        string[] tables = [orders, stock, events, outbox];
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
            foreach (var table in tables) await InsertAsync(table, id);
            if (fail) throw new InvalidOperationException("Failure after outbox insertion");
            return id;
        }, ct);

        // Include attempted writes so a previous failed run cannot leave fixture rows behind.
        await unitOfWork.RunInManagedSessionAsync(async token => {
            await ExecuteAsync($"DELETE FROM {orders} WHERE id IN (1, 2, 3, 4, 7)");
            await ExecuteAsync($"DELETE FROM {stock} WHERE id IN (1, 2, 3, 4)");
            await ExecuteAsync($"DELETE FROM {events} WHERE id IN (1, 2, 3, 4, 8)");
            await ExecuteAsync($"DELETE FROM {outbox} WHERE id IN (1, 2, 3, 5, 6)");
        }, ct);

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
            await InsertAsync(orders, 4);
            await InsertAsync(stock, 4);
            await InsertAsync(events, 4);
            await InsertAsync(outbox, 1);
        }, ct));
        foreach (var table in tables) Assert.Equal(1, await CountAsync(table));

        // A savepoint can recover a statement-level duplicate without committing the outer transaction.
        await unitOfWork.RunInManagedSessionAsync(async token => {
            await ExecuteAsync("SAVEPOINT test_duplicate");
            await InsertAsync(orders, 7);
            var duplicate = await Record.ExceptionAsync(() => InsertAsync(outbox, 1));
            Assert.NotNull(duplicate);
            Assert.True(isUniqueViolation(duplicate));
            await ExecuteAsync("ROLLBACK TO SAVEPOINT test_duplicate");
            if (!oracle) await ExecuteAsync("RELEASE SAVEPOINT test_duplicate");
            await InsertAsync(events, 8);
        }, ct);
        Assert.Equal(1, await CountAsync(orders));
        Assert.Equal(2, await CountAsync(events));

        // Exercise a test-owned outbox lifecycle, including a batch of entries, publication and cleanup.
        await unitOfWork.RunInManagedSessionAsync(async token => {
            await InsertAsync(outbox, 5);
            await InsertAsync(outbox, 6);
            await ExecuteAsync($"UPDATE {outbox} SET published = 1, age_days = 31 WHERE id = 5");
            await ExecuteAsync($"UPDATE {outbox} SET published = 1 WHERE id = 6");
        }, ct);
        Assert.Equal(1, await ExecuteAsync($"DELETE FROM {outbox} WHERE id IN (1, 5, 6) AND published = 1 AND age_days > 30"));
        Assert.Equal(1, await CountAsync(outbox, "WHERE published = 0"));
        Assert.Equal(1, await CountAsync(outbox, "WHERE published = 1"));
    }

    private sealed class TestConnectionPool<TConnection>(Func<TConnection> create)
        : DbConnectionPoolBase<TConnection>(DataStorageInstanceType.Writer, "") where TConnection : DbConnection
    {
        protected override TConnection CreateConnection(string connectionString) => create();
    }
}
