using Anarchy.Toolkit.Core.Persistence.Abstractions.Model;
using Anarchy.Toolkit.Testing;

namespace Anarchy.Toolkit.Tests.Unit.Core.Persistence.Abstractions.Model;

public class AttributeIdCacheBaseTest : TestCaseBase
{
    private enum TestAttribute
    {
        First,
        Second
    }

    public AttributeIdCacheBaseTest()
    {
        AttributeIdCacheBase<TestAttribute, Guid>.Clear();
        AttributeIdCacheBase<TestAttribute, string>.Clear();
        AttributeIdCacheBase<TestAttribute, long>.Clear();
    }

    [Fact]
    public async Task GetOrAddAsync_WhenGuidIsHydrated_ReturnsAndCachesIt()
    {
        var id = Guid.NewGuid();
        var hydrations = 0;
        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, Guid>.Dto>> Hydrate(
            TestAttribute attribute, CancellationToken? token)
        {
            hydrations++;
            return Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, Guid>.Dto>>(
                [new(id, "name")]);
        }

        Task<Guid> Insert(TestAttribute attribute, string name, CancellationToken? token) =>
            throw new InvalidOperationException("Unexpected insert");

        var first = await AttributeIdCacheBase<TestAttribute, Guid>.GetOrAddAsync(
            TestAttribute.First, "name", Hydrate, Insert, TestContext.Current.CancellationToken);
        var second = await AttributeIdCacheBase<TestAttribute, Guid>.GetOrAddAsync(
            TestAttribute.First, "name", Hydrate, Insert, TestContext.Current.CancellationToken);

        Assert.Equal(id, first);
        Assert.Equal(id, second);
        Assert.Equal(1, hydrations);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenStringIdIsInserted_CachesPerAttribute()
    {
        var inserts = 0;
        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, string>.Dto>> Hydrate(
            TestAttribute attribute, CancellationToken? token) =>
            Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, string>.Dto>>([]);
        Task<string> Insert(TestAttribute attribute, string name, CancellationToken? token)
        {
            inserts++;
            return Task.FromResult(attribute.ToString());
        }

        var first = await AttributeIdCacheBase<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.First, "name", Hydrate, Insert, TestContext.Current.CancellationToken);
        var second = await AttributeIdCacheBase<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.Second, "name", Hydrate, Insert, TestContext.Current.CancellationToken);
        var cached = await AttributeIdCacheBase<TestAttribute, string>.GetOrAddAsync(
            TestAttribute.First, "name", Hydrate, Insert, TestContext.Current.CancellationToken);

        Assert.Equal("First", first);
        Assert.Equal("Second", second);
        Assert.Equal(first, cached);
        Assert.Equal(2, inserts);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenConcurrentInsertsResolveSameId_CachesThatId()
    {
        const int callerCount = 8;
        var inserts = 0;
        var allInsertsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, long>.Dto>> Hydrate(
            TestAttribute attribute, CancellationToken? token) =>
            Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, long>.Dto>>([]);
        async Task<long> Insert(TestAttribute attribute, string name, CancellationToken? token)
        {
            if (Interlocked.Increment(ref inserts) == callerCount)
            {
                allInsertsStarted.SetResult();
            }

            await allInsertsStarted.Task.WaitAsync(token ?? CancellationToken.None);
            return 42L;
        }

        var calls = Enumerable.Range(0, callerCount).Select(_ =>
            AttributeIdCacheBase<TestAttribute, long>.GetOrAddAsync(
                TestAttribute.First, "name", Hydrate, Insert, TestContext.Current.CancellationToken)).ToArray();
        var results = await Task.WhenAll(calls);
        var cached = await AttributeIdCacheBase<TestAttribute, long>.GetOrAddAsync(
            TestAttribute.First, "name", Hydrate, Insert, TestContext.Current.CancellationToken);

        Assert.All(results, id => Assert.Equal(42L, id));
        Assert.Equal(42L, cached);
        Assert.Equal(callerCount, inserts);
    }

    [Fact]
    public async Task Clear_WhenHydrationIsInFlight_DoesNotRepopulateCurrentCache()
    {
        var hydrationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishHydration = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, long>.Dto>> OldHydrate(
            TestAttribute attribute, CancellationToken? token)
        {
            hydrationStarted.SetResult();
            await finishHydration.Task.WaitAsync(token ?? CancellationToken.None);
            return [new(1L, "name")];
        }

        Task<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, long>.Dto>> NewHydrate(
            TestAttribute attribute, CancellationToken? token) =>
            Task.FromResult<IReadOnlyCollection<AttributeIdCacheBase<TestAttribute, long>.Dto>>([new(2L, "name")]);
        Task<long> Insert(TestAttribute attribute, string name, CancellationToken? token) =>
            throw new InvalidOperationException("Unexpected insert");

        var oldLookup = AttributeIdCacheBase<TestAttribute, long>.GetOrAddAsync(
            TestAttribute.First, "name", OldHydrate, Insert, TestContext.Current.CancellationToken);
        await hydrationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        AttributeIdCacheBase<TestAttribute, long>.Clear();
        var newId = await AttributeIdCacheBase<TestAttribute, long>.GetOrAddAsync(
            TestAttribute.First, "name", NewHydrate, Insert, TestContext.Current.CancellationToken);
        finishHydration.SetResult();
        var oldId = await oldLookup;
        var cachedId = await AttributeIdCacheBase<TestAttribute, long>.GetOrAddAsync(
            TestAttribute.First, "name", NewHydrate, Insert, TestContext.Current.CancellationToken);

        Assert.Equal(1L, oldId);
        Assert.Equal(2L, newId);
        Assert.Equal(newId, cachedId);
    }
}
