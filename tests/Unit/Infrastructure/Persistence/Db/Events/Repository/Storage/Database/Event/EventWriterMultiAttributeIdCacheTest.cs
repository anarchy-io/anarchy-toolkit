using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;
using Anarchy.Toolkit.Testing;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;

public class EventWriterMultiAttributeIdCacheTest : TestCaseBase
{
    private const EventTypeTable Table = EventTypeTable.TypeEvent;
    private const string TypeName = "ExampleEvent";

    [Fact]
    public async Task GetOrAddAsync_WhenDatabasesHaveDifferentIds_UsesEachDatabase()
    {
        var first = await ResolveExistingAsync(11);
        var second = await ResolveExistingAsync(22);
        var firstAgain = await ResolveExistingAsync(11);

        Assert.Equal(11, first);
        Assert.Equal(22, second);
        Assert.Equal(11, firstAgain);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenHydrationSeesUncommittedRow_DoesNotPublishIt()
    {
        var transactionId = await ResolveExistingAsync(11);
        var otherTransactionId = await ResolveExistingAsync(22);

        Assert.Equal(11, transactionId);
        Assert.Equal(22, otherTransactionId);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenInsertionIsUncommittedOrRolledBack_DoesNotReuseItsId()
    {
        var token = TestContext.Current.CancellationToken;
        var insertedId = await EventWriterMultiAttributeIdCache.GetOrAddAsync(
            Table, TypeName, EmptyAsync, (_, _, _) => Task.FromResult(11), token);

        // A separate transaction does not see the first transaction's uncommitted row.
        var concurrentId = await EventWriterMultiAttributeIdCache.GetOrAddAsync(
            Table, TypeName, EmptyAsync, (_, _, _) => Task.FromResult(22), token);

        // The first transaction rolls back; its row is no longer visible in the backing store.
        var afterRollback = await EventWriterMultiAttributeIdCache.GetOrAddAsync(
            Table, TypeName, EmptyAsync, (_, _, _) => Task.FromResult(33), token);

        Assert.Equal(11, insertedId);
        Assert.Equal(22, concurrentId);
        Assert.Equal(33, afterRollback);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenFirstRegistrationIsConcurrent_UsesDatabaseGetOrInsertForBoth()
    {
        var token = TestContext.Current.CancellationToken;
        var bothHydrated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hydrationCount = 0;
        var insertCount = 0;
        var rows = new ConcurrentDictionary<string, int>();

        async Task<IReadOnlyCollection<EventWriterMultiAttributeIdCache.Dto>> HydrateAsync(
            EventTypeTable _, CancellationToken? cancellationToken)
        {
            if (Interlocked.Increment(ref hydrationCount) == 2)
            {
                bothHydrated.SetResult();
            }

            await bothHydrated.Task.WaitAsync(cancellationToken ?? CancellationToken.None);
            return [];
        }

        Task<int> GetOrInsertAsync(EventTypeTable _, string name, CancellationToken? cancellationToken)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            Interlocked.Increment(ref insertCount);
            return Task.FromResult(rows.GetOrAdd(name, 42));
        }

        var first = EventWriterMultiAttributeIdCache.GetOrAddAsync(Table, TypeName, HydrateAsync, GetOrInsertAsync, token);
        var second = EventWriterMultiAttributeIdCache.GetOrAddAsync(Table, TypeName, HydrateAsync, GetOrInsertAsync, token);
        var results = await Task.WhenAll(first, second);

        Assert.Equal(new[] { 42, 42 }, results);
        Assert.Equal(2, insertCount);
        Assert.Single(rows);
    }

    private static Task<int> ResolveExistingAsync(int id)
    {
        return EventWriterMultiAttributeIdCache.GetOrAddAsync(
            Table,
            TypeName,
            (_, _) => Task.FromResult<IReadOnlyCollection<EventWriterMultiAttributeIdCache.Dto>>(
                [new(id, TypeName)]),
            (_, _, _) => throw new InvalidOperationException("The row should already exist."),
            TestContext.Current.CancellationToken);
    }

    private static Task<IReadOnlyCollection<EventWriterMultiAttributeIdCache.Dto>> EmptyAsync(
        EventTypeTable _, CancellationToken? cancellationToken)
    {
        cancellationToken?.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<EventWriterMultiAttributeIdCache.Dto>>([]);
    }
}
