using Anarchy.Toolkit.Core.Persistence.Abstractions.Model;
using Anarchy.Toolkit.Testing;

namespace Anarchy.Toolkit.Tests.Unit.Core.Persistence.Abstractions.Model;

public class AttributeIdResolverTest : TestCaseBase
{
    private enum TestAttribute
    {
        First
    }

    [Fact]
    public async Task GetOrAddAsync_WhenRowExists_ReturnsVisibleIdAndForwardsToken()
    {
        var id = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, Guid>.Dto>> Hydrate(
            TestAttribute attribute, CancellationToken? callbackToken)
        {
            Assert.Equal(TestAttribute.First, attribute);
            Assert.Equal(token, callbackToken);
            return Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, Guid>.Dto>>(
                [new(id, "name")]);
        }

        Task<Guid> Insert(TestAttribute attribute, string name, CancellationToken? callbackToken) =>
            throw new InvalidOperationException("Unexpected insert");

        var result = await AttributeIdResolver<TestAttribute, Guid>.GetOrAddAsync(
            TestAttribute.First, "name", Hydrate, Insert, token);

        Assert.Equal(id, result);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenNameDiffersInCase_InsertsAndForwardsToken()
    {
        var token = TestContext.Current.CancellationToken;
        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, string>.Dto>> Hydrate(
            TestAttribute attribute, CancellationToken? callbackToken) =>
            Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, string>.Dto>>(
                [new("existing", "NAME")]);
        Task<string> Insert(TestAttribute attribute, string name, CancellationToken? callbackToken)
        {
            Assert.Equal(TestAttribute.First, attribute);
            Assert.Equal("name", name);
            Assert.Equal(token, callbackToken);
            return Task.FromResult("inserted");
        }

        var result = await AttributeIdResolver<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.First, "name", Hydrate, Insert, token);

        Assert.Equal("inserted", result);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenPreviousTransactionRollsBack_DoesNotReuseItsInsertedId()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var insertCount = 0;
        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, Guid>.Dto>> Empty(
            TestAttribute attribute, CancellationToken? token) =>
            Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, Guid>.Dto>>([]);
        Task<Guid> Insert(TestAttribute attribute, string name, CancellationToken? token) =>
            Task.FromResult(++insertCount == 1 ? firstId : secondId);

        // The first result represents a transaction-local insert that the caller subsequently rolls back.
        var rolledBackId = await AttributeIdResolver<TestAttribute, Guid>.GetOrAddAsync(
            TestAttribute.First, "name", Empty, Insert, TestContext.Current.CancellationToken);
        var nextId = await AttributeIdResolver<TestAttribute, Guid>.GetOrAddAsync(
            TestAttribute.First, "name", Empty, Insert, TestContext.Current.CancellationToken);

        Assert.Equal(firstId, rolledBackId);
        Assert.Equal(secondId, nextId);
        Assert.Equal(2, insertCount);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenInsertIsUncommitted_AnotherCallUsesItsOwnCallbacks()
    {
        var firstInsertStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishFirstInsert = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, string>.Dto>> Empty(
            TestAttribute attribute, CancellationToken? token) =>
            Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, string>.Dto>>([]);
        async Task<string> FirstInsert(TestAttribute attribute, string name, CancellationToken? token)
        {
            firstInsertStarted.SetResult();
            await finishFirstInsert.Task.WaitAsync(token ?? CancellationToken.None);
            return "uncommitted";
        }

        Task<string> SecondInsert(TestAttribute attribute, string name, CancellationToken? token) =>
            Task.FromResult("other-database");

        var first = AttributeIdResolver<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.First, "name", Empty, FirstInsert, TestContext.Current.CancellationToken);
        await firstInsertStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = await AttributeIdResolver<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.First, "name", Empty, SecondInsert, TestContext.Current.CancellationToken);
        finishFirstInsert.SetResult();

        Assert.Equal("uncommitted", await first);
        Assert.Equal("other-database", second);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenConcurrentCallsUseDifferentDatabases_DoesNotShareHydratedIds()
    {
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        async Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, string>.Dto>> Hydrate(
            string id, CancellationToken? token)
        {
            if (Interlocked.Increment(ref started) == 2)
            {
                bothStarted.SetResult();
            }

            await bothStarted.Task.WaitAsync(token ?? CancellationToken.None);
            return [new(id, "name")];
        }

        Task<string> Insert(TestAttribute attribute, string name, CancellationToken? token) =>
            throw new InvalidOperationException("Unexpected insert");

        var first = AttributeIdResolver<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.First, "name", (attribute, token) => Hydrate("database-one", token), Insert,
            TestContext.Current.CancellationToken);
        var second = AttributeIdResolver<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.First, "name", (attribute, token) => Hydrate("database-two", token), Insert,
            TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(first, second);

        Assert.Equal("database-one", results[0]);
        Assert.Equal("database-two", results[1]);
        Assert.Equal(2, started);
    }
}
