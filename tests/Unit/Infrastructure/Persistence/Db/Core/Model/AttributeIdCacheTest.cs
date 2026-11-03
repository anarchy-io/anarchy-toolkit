using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Model;
using Anarchy.Toolkit.Testing;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Core.Model;

public class AttributeIdCacheTest : TestCaseBase
{
    public enum TestAttribute
    {
        TypeA,
        TypeB
    }

    public AttributeIdCacheTest()
    {
        // Ensure the static cache is clean before each test
        AttributeIdCacheBase<TestAttribute, int>.Clear();
    }

    [Fact]
    public async Task GetOrAddAsync_WhenCacheEmpty_UsesHydrateAllAndReturnsExistingId()
    {
        // Arrange
        var attribute = TestAttribute.TypeA;
        var requestedName = "existing";
        var expectedId = 42;

        var mockHydrate =
            new Mock<AttributeIdCacheBase<TestAttribute, int>.HydrateAllAsync>();

        var mockInsert =
            new Mock<AttributeIdCacheBase<TestAttribute, int>.InsertMissingAsync>();

        mockHydrate
            .Setup(h => h(attribute))
            .ReturnsAsync(
                new List<AttributeIdCacheBase<TestAttribute, int>.Dto> { new(expectedId, requestedName) });

        mockInsert
            .Setup(i => i(It.IsAny<TestAttribute>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("InsertSingleAsync should not be called"));

        // Act
        var result = await AttributeIdCacheBase<TestAttribute, int>.GetOrAddAsync(
            attribute,
            requestedName,
            mockHydrate.Object,
            mockInsert.Object);

        // Assert
        Assert.Equal(expectedId, result);
        mockHydrate.Verify(h => h(attribute), Times.Once);
        mockInsert.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetOrAddAsync_WhenValueMissingAfterHydration_InsertsAndCaches()
    {
        // Arrange
        var attribute = TestAttribute.TypeA;
        var requestedName = "new-value";
        var newId = 100;

        var mockHydrate =
            new Mock<AttributeIdCacheBase<TestAttribute, int>.HydrateAllAsync>();

        var mockInsert =
            new Mock<AttributeIdCacheBase<TestAttribute, int>.InsertMissingAsync>();

        // No rows for this attribute initially
        mockHydrate
            .Setup(h => h(attribute))
            .ReturnsAsync(Array.Empty<AttributeIdCacheBase<TestAttribute, int>.Dto>());

        mockInsert
            .Setup(i => i(attribute, requestedName))
            .ReturnsAsync(newId);

        // Act: first call hydrates (empty) then inserts
        var first = await AttributeIdCacheBase<TestAttribute, int>.GetOrAddAsync(
            attribute,
            requestedName,
            mockHydrate.Object,
            mockInsert.Object);

        // Act: second call should hit the in-memory cache only
        var second = await AttributeIdCacheBase<TestAttribute, int>.GetOrAddAsync(
            attribute,
            requestedName,
            mockHydrate.Object,
            mockInsert.Object);

        // Assert
        Assert.Equal(newId, first);
        Assert.Equal(newId, second);

        mockHydrate.Verify(h => h(attribute), Times.Once);
        mockInsert.Verify(i => i(attribute, requestedName), Times.Once);
    }

    [Fact]
    public async Task Clear_RemovesCachedValues_ForcingRehydration()
    {
        // Arrange
        var attribute = TestAttribute.TypeA;
        var requestedName = "existing";
        var expectedId = 7;

        var mockHydrate =
            new Mock<AttributeIdCacheBase<TestAttribute, int>.HydrateAllAsync>();

        var mockInsert =
            new Mock<AttributeIdCacheBase<TestAttribute, int>.InsertMissingAsync>();

        mockHydrate
            .Setup(h => h(attribute))
            .ReturnsAsync(
                new List<AttributeIdCacheBase<TestAttribute, int>.Dto> { new(expectedId, requestedName) });

        // Act: first call primes the cache
        var first = await AttributeIdCacheBase<TestAttribute, int>.GetOrAddAsync(
            attribute,
            requestedName,
            mockHydrate.Object,
            mockInsert.Object);

        // Clear the underlying static cache
        AttributeIdCacheBase<TestAttribute, int>.Clear();

        // Act: second call should rehydrate again
        var second = await AttributeIdCacheBase<TestAttribute, int>.GetOrAddAsync(
            attribute,
            requestedName,
            mockHydrate.Object,
            mockInsert.Object);

        // Assert
        Assert.Equal(expectedId, first);
        Assert.Equal(expectedId, second);

        // Hydration is called once before Clear, once after Clear
        mockHydrate.Verify(h => h(attribute), Times.Exactly(2));
        mockInsert.VerifyNoOtherCalls();
    }
}
