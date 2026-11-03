using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Testing;

namespace Anarchy.Toolkit.Tests.Unit.Core.Helper;

public class ThreadHelperTest : TestCaseBase
{
    [Fact]
    public async Task ExecuteInParallelWithThrottlingAsync_AllChunksSucceed_ReturnsFlattenedResults()
    {
        // Arrange
        var inputs = Enumerable.Range(1, 5)
            .Select(i => new[] { i * 10, i * 10 + 1 })
            .ToList();

        async Task<IReadOnlyCollection<string>> DummyWorkAsync(int[] chunk)
        {
            await Task.Delay(20);
            return [.. chunk.Select(x => $"Result-{x}")];
        }

        // Act
        var results = await ThreadHelper.ExecuteInParallelWithThrottlingAsync(
            callbackArgsCollection: inputs,
            callbackFunc: DummyWorkAsync,
            maxConcurrentExecutions: 3,
            cancellationToken: CancellationToken.None
        );

        // Assert
        var result = results.Flatten();
        Assert.Equal(10, result.Count);
        Assert.All(result, r => Assert.StartsWith("Result-", r));
    }

    [Fact]
    public async Task ExecuteInParallelWithThrottlingAsync_OneChunkFails_ThrowsAndCancelsOthers()
    {
        // Arrange
        var inputs = Enumerable.Range(1, 6)
            .Select(i => new[] { i })
            .ToList();

        async Task<IReadOnlyCollection<string>> MaybeFailAsync(int[] chunk)
        {
            var id = chunk[0];
            await Task.Delay(10);
            if (id == 4)
                throw new InvalidOperationException("Fail on 4");
            return [$"Processed-{id}"];
        }

        var cts = new CancellationTokenSource();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await ThreadHelper.ExecuteInParallelWithThrottlingAsync(
                callbackArgsCollection: inputs,
                callbackFunc: MaybeFailAsync,
                maxConcurrentExecutions: 2,
                cancellationToken: cts.Token
            );
        });

        Assert.Equal("Fail on 4", ex.Message);
    }

    [Fact]
    public async Task ExecuteInParallelWithThrottlingAsync_ThrottlingIsRespected()
    {
        // Arrange
        var inputs = Enumerable.Range(1, 8)
            .Select(i => new[] { i })
            .ToList();

        var maxObserved = 0;
        var current = 0;
        var lockObj = new object();

        async Task<IReadOnlyCollection<string>> MonitoredTaskAsync(int[] chunk)
        {
            lock (lockObj)
            {
                current++;
                maxObserved = Math.Max(maxObserved, current);
            }

            await Task.Delay(50);

            lock (lockObj)
            {
                current--;
            }

            return [$"Done-{chunk[0]}"];
        }

        // Act
        var result = await ThreadHelper.ExecuteInParallelWithThrottlingAsync(
            callbackArgsCollection: inputs,
            callbackFunc: MonitoredTaskAsync,
            maxConcurrentExecutions: 3,
            cancellationToken: CancellationToken.None
        );

        // Assert
        Assert.Equal(inputs.Count, result.Count);
        Assert.InRange(maxObserved, 1, 3);
    }

    [Fact]
    public async Task ExecuteInParallelWithThrottlingAsync_EmptyInput_ReturnsEmpty()
    {
        // Act
        var result = await ThreadHelper.ExecuteInParallelWithThrottlingAsync(
            callbackArgsCollection: new List<int[]>(),
            callbackFunc: async _ => await Task.FromResult<IReadOnlyCollection<string>>(["Unused"]),
            maxConcurrentExecutions: 2,
            cancellationToken: CancellationToken.None
        );

        // Assert
        Assert.Empty(result);
    }
}
