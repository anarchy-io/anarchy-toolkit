using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Testing.Telemetry;
using Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.Adapters;
using Dapper;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using Mysql = Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.Adapters.Mysql;
using OracleAdapter = Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.Adapters.Oracle;
using Postgres = Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.Adapters.Postgres;

namespace Anarchy.Toolkit.Tests.Integration.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;

/// <summary>Runs toolkit repository bases against the event-store fixture installed by Compose.</summary>
internal static class EventStoreScenario
{
    public static Task RunPostgresAsync(Func<NpgsqlConnection> connectionFactory) => RunAsync(connectionFactory,
        (pool, context) => new PostgresDbUnitOfWork(pool, context),
        (policy, telemetry) => new AdapterSet(new Postgres.TestOrderWriter(policy, telemetry),
            new Postgres.TestOrderReader(policy, telemetry), new Postgres.TestEventWriter(policy, telemetry),
            new Postgres.TestEventReader(policy, telemetry)),
        PostgresDbDialect.IsUniqueViolation);

    public static Task RunMysqlAsync(Func<MySqlConnection> connectionFactory) => RunAsync(connectionFactory,
        (pool, context) => new MysqlDbUnitOfWork(pool, context),
        (policy, telemetry) => new AdapterSet(new Mysql.TestOrderWriter(policy, telemetry),
            new Mysql.TestOrderReader(policy, telemetry), new Mysql.TestEventWriter(policy, telemetry),
            new Mysql.TestEventReader(policy, telemetry)),
        MysqlDbDialect.IsUniqueViolation);

    public static Task RunOracleAsync(Func<OracleConnection> connectionFactory) => RunAsync(connectionFactory,
        (pool, context) => new OracleDbUnitOfWork(pool, context),
        (policy, telemetry) => new AdapterSet(new OracleAdapter.TestOrderWriter(policy, telemetry),
            new OracleAdapter.TestOrderReader(policy, telemetry), new OracleAdapter.TestEventWriter(policy, telemetry),
            new OracleAdapter.TestEventReader(policy, telemetry)),
        OracleDbDialect.IsUniqueViolation);

    private static async Task RunAsync<TConnection>(
        Func<TConnection> connectionFactory,
        Func<IDbConnectionPool<TConnection>, IAsyncFlowContext<StorageConnectionEnvelopeBase<TConnection>>, IUnitOfWork> createUnitOfWork,
        Func<IStoragePolicy<TConnection>, AppTelemetry, AdapterSet> createAdapters,
        Func<Exception, bool> isUniqueViolation)
        where TConnection : DbConnection
    {
        var ct = TestContext.Current.CancellationToken;
        await using var pool = new TestConnectionPool<TConnection>(connectionFactory);
        var context = new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<TConnection>>();
        var policy = new DbPolicy<TConnection>(pool, context);
        var owner = createUnitOfWork(pool, context);
        using var telemetry = new TestTelemetry();
        var adapters = createAdapters(policy, telemetry.Telemetry);

        async Task<int> ExecuteAsync(string sql) => await policy.ExecuteAsync(
            async (connection, token) => await connection.DataStorageConnection.ExecuteAsync(
                new CommandDefinition(sql, cancellationToken: token ?? CancellationToken.None)), cancellationToken: ct);
        async Task<int> CountAsync(string sql) => await policy.ExecuteAsync(
            async (connection, token) => await connection.DataStorageConnection.QuerySingleAsync<int>(
                new CommandDefinition(sql, cancellationToken: token ?? CancellationToken.None)), cancellationToken: ct);

        // Clear only the fixtures from a previous run. Delete events before the type rows they reference.
        await owner.RunInManagedSessionAsync(async token => {
            await ExecuteAsync("""
                DELETE FROM domain_event
                WHERE aggregate_id IN ('order-1', 'order-2', 'order-3')
                  AND type_aggregate_id IN (SELECT id FROM type_registry WHERE name = 'OrderType')
                  AND type_event_id IN (SELECT id FROM type_registry WHERE name = 'OrderPlaced')
                """);
            await ExecuteAsync("DELETE FROM test_order WHERE id IN (1, 2, 3)");
            await ExecuteAsync("DELETE FROM type_registry WHERE name IN ('ConcurrentType', 'UncommittedType', 'OrderType', 'OrderPlaced')");
        }, ct);

        // Both database hydrations must finish before either insert, so this exercises the first-registration race
        // rather than merely scheduling two tasks which may happen to run serially.
        var first = createUnitOfWork(pool, context);
        var second = createUnitOfWork(pool, context);
        var racingEvents = createAdapters(new HydrationBarrierPolicy<TConnection>(policy), telemetry.Telemetry).Events;
        Task<int> RegisterAsync(IUnitOfWork uow) => uow.RunInManagedSessionAsync(
            token => racingEvents.GetIdForTypeAsync(EventTypeTable.TypeAggregate, "ConcurrentType", token), ct);
        var ids = await Task.WhenAll(RegisterAsync(first), RegisterAsync(second));
        Assert.Equal(ids[0], ids[1]);
        Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM type_registry WHERE name = 'ConcurrentType'"));

        // Keep a registration uncommitted while a second transaction hydrates. The second must consult its own
        // database view rather than reuse the first transaction's identifier; after rollback it registers the name.
        var firstRegistered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondHydrated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observerEvents = createAdapters(new HydrationNotificationPolicy<TConnection>(policy, secondHydrated), telemetry.Telemetry).Events;
        var rolledBackRegistration = first.RunInManagedSessionAsync<int>(async token => {
            await adapters.Events.GetIdForTypeAsync(EventTypeTable.TypeAggregate, "UncommittedType", token);
            firstRegistered.TrySetResult();
            await secondHydrated.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            throw new InvalidOperationException("roll back type registration");
        }, ct);
        await firstRegistered.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        var committedRegistration = second.RunInManagedSessionAsync(
            token => observerEvents.GetIdForTypeAsync(EventTypeTable.TypeAggregate, "UncommittedType", token), ct);
        await Assert.ThrowsAsync<SessionCallbackException>(() => rolledBackRegistration);
        var committedId = await committedRegistration;
        Assert.True(committedId > 0);
        Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM type_registry WHERE name = 'UncommittedType'"));

        var event1 = new DomainEventEnvelope("OrderType", "order-1", "OrderPlaced", new { Sku = "A" }, customAttributes:
            new Dictionary<string, string> { ["tenant"] = "west \"A\"", ["Tenant"] = "北" });
        var event2 = new DomainEventEnvelope("OrderType", "order-2", "OrderPlaced", new { Sku = "B" });
        var event3 = new DomainEventEnvelope("OrderType", "order-3", "OrderPlaced", new { Sku = "C" });

        Task AggregateAsync(IReadOnlyCollection<NewTestOrder> orders, IReadOnlyCollection<DomainEventEnvelope> events,
                             bool failAfterEventWrite = false) =>
            owner.RunInJoinedOrManagedSessionAsync(async token => {
                await adapters.Orders.SaveAllAsync(orders, token);
                await adapters.Events.SaveAllAsync(events, token);
                if (failAfterEventWrite) throw new InvalidOperationException("after event write");
                return true;
            }, ct);

        NewTestOrder[] batch = [new(1, "A"), new(2, "B")];
        DomainEventEnvelope[] events = [event1, event2];
        await Assert.ThrowsAsync<SessionCallbackException>(() => AggregateAsync(batch, events, failAfterEventWrite: true));
        await Assert.ThrowsAsync<SessionCallbackException>(() => owner.RunInManagedSessionAsync(async token => {
            await AggregateAsync(batch, events);
            throw new InvalidOperationException("caller failed after aggregate returned");
        }, ct));
        Assert.Empty(await adapters.OrderReader.GetAllAsync(cancellationToken: ct));
        Assert.Empty(await adapters.EventReader.GetAllAsync(cancellationToken: ct));
        Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM domain_event"));
        Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM type_registry WHERE name IN ('OrderType', 'OrderPlaced')"));

        // Caller-owned transaction; the aggregate joins, and the same type names are registered again after rollback.
        await owner.RunInManagedSessionAsync(async token => await AggregateAsync(batch, events), ct);
        Assert.Equal(2, (await adapters.OrderReader.GetAllAsync(cancellationToken: ct)).Count);
        Assert.Equal("A", (await adapters.OrderReader.GetByIdAsync(1, ct)).Sku);
        Assert.Equal(2, await CountAsync("SELECT COUNT(*) FROM domain_event"));
        var storedEvents = (await adapters.EventReader.GetAllAsync(cancellationToken: ct)).OrderBy(x => x.Id).ToArray();
        Assert.Equal(2, storedEvents.Length);
        Assert.Equal("order-1", storedEvents[0].Subject);
        Assert.Equal("OrderPlaced", storedEvents[0].Type);
        Assert.Equal("A", storedEvents[0].CastData<TestPayload>().Sku);
        Assert.True((storedEvents[0].Time - event1.Time).Duration() < TimeSpan.FromSeconds(1));
        AssertAttributes(event1, storedEvents[0]);
        Assert.Empty(storedEvents[1].CustomAttributes);
        Assert.Equal(storedEvents[1].Id, Assert.Single(
            await adapters.EventReader.GetAllAsync(limit: 1, offset: 1, cancellationToken: ct)).Id);

        // Rows from before the optional column was introduced can have a SQL NULL instead of an empty object.
        await ExecuteAsync($"UPDATE domain_event SET custom_attributes = NULL WHERE id = {storedEvents[1].Id}");
        Assert.Empty(Assert.Single(await adapters.EventReader.GetAllAsync(cancellationToken: ct),
            stored => stored.Id == storedEvents[1].Id).CustomAttributes);

        // The fixture allows one event per aggregate and event type; a duplicate rolls back the preceding writes.
        var duplicate = await Assert.ThrowsAsync<SessionCallbackException>(() => AggregateAsync([new(3, "C")], [event3, event1]));
        Assert.NotNull(duplicate.InnerException);
        Assert.True(isUniqueViolation(duplicate.InnerException));
        Assert.Equal(2, (await adapters.OrderReader.GetAllAsync(cancellationToken: ct)).Count);
        Assert.Equal(2, await CountAsync("SELECT COUNT(*) FROM domain_event"));

        // Without a caller-owned session, the aggregate commits both repositories itself.
        await AggregateAsync([new(3, "C")], [event3]);
        Assert.Equal("C", (await adapters.OrderReader.GetByIdAsync(3, ct)).Sku);
        Assert.Equal("C", Assert.Single(await adapters.EventReader.GetAllAsync(cancellationToken: ct),
            stored => stored.Subject == event3.Subject).CastData<TestPayload>().Sku);
    }

    private static void AssertAttributes(IEventEnvelope expected, IEventEnvelope actual)
    {
        Assert.Equal(expected.CustomAttributes.Count, actual.CustomAttributes.Count);
        foreach (var (name, value) in expected.CustomAttributes)
        {
            Assert.Equal(value, actual.CustomAttributes[name]);
        }
    }

    private sealed record AdapterSet(
        IStorageBatchWriterRepository<NewTestOrder> Orders,
        IStorageReaderRepository<TestOrder> OrderReader,
        IEventEnvelopeWriterRepository<DomainEventEnvelope> Events,
        IStorageReaderRepository<PersistedDomainEvent> EventReader);

    private sealed record TestPayload(string Sku);

    private sealed class TestConnectionPool<TConnection>(Func<TConnection> create)
        : DbConnectionPoolBase<TConnection>(DataStorageInstanceType.Writer, "") where TConnection : DbConnection
    {
        protected override TConnection CreateConnection(string connectionString) => create();
    }

    private sealed class HydrationBarrierPolicy<TConnection>(IStoragePolicy<TConnection> inner) : IStoragePolicy<TConnection>
    {
        private readonly TaskCompletionSource _hydrated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _remaining = 2;

        public async Task<TResult> ExecuteAsync<TResult>(StoragePolicyCallback<TConnection, TResult> callback,
                                                         string? operationName = null,
                                                         CancellationToken? cancellationToken = null)
        {
            var result = await inner.ExecuteAsync(callback, operationName, cancellationToken);
            if (operationName == "Common.TypeAggregate.Get.All")
            {
                if (Interlocked.Decrement(ref _remaining) == 0) _hydrated.TrySetResult();
                await _hydrated.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken ?? CancellationToken.None);
            }
            return result;
        }
    }

    private sealed class HydrationNotificationPolicy<TConnection>(IStoragePolicy<TConnection> inner, TaskCompletionSource hydrated)
        : IStoragePolicy<TConnection>
    {
        public async Task<TResult> ExecuteAsync<TResult>(StoragePolicyCallback<TConnection, TResult> callback,
                                                         string? operationName = null,
                                                         CancellationToken? cancellationToken = null)
        {
            var result = await inner.ExecuteAsync(callback, operationName, cancellationToken);
            if (operationName == "Common.TypeAggregate.Get.All") hydrated.TrySetResult();
            return result;
        }
    }
}
