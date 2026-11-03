using Anarchy.Toolkit.Core.Observability.Service.Observability;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Observability.Core.Service.Observability;

public class MetricsTests : MeasurerBase
{
    [Fact]
    public void StartTimerWithNullNameShouldThrow()
    {
        this.Invoking(obj => obj.StartTimer(name: null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartTimerWithEmptyNameShouldThrow()
    {
        this.Invoking(obj => obj.StartTimer(name: ""))
            .Should().Throw<ArgumentException>()
            .Which.Message.Should()
            .ContainEquivalentOf("name cannot be empty");
    }

    [Fact]
    public void StartTimerWithNullTagsShouldThrow()
    {
        this.Invoking(obj => obj.StartTimer(name: "timer", tags: null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartTimerWithSingleNullTagShouldThrow()
    {
        this.Invoking(obj => obj.StartTimer(name: "timer", tags: [null!]))
            .Should().Throw<ArgumentException>()
            .Which.Message.Should()
            .ContainEquivalentOf("tag cannot be null");
    }

    [Fact]
    public void StartTimerWhenDisposedCallsTimerMethod()
    {
        var delay = TimeSpan.FromSeconds(1);

        using (StartTimer("timer", "tag1", "tag2"))
        {
            Thread.Sleep(delay);
        }

        _providedTimerName.Should().Be("timer");
        _providedTags.Should().BeEquivalentTo("tag1", "tag2");
        _providedElapsedTime.Should().BeCloseTo(delay, precision: TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void StartTimerWhenDisposedMoreThanOnceOnlyCallsTimerMethodOnce()
    {
        using (var disposable = StartTimer("timer", "tag1", "tag2"))
        {
            disposable.Dispose();
        }

        _timerCallCount.Should().Be(1);
    }

    [Fact]
    public void StartTimerWithSuccessTagAndNullNameShouldThrow()
    {
        this.Invoking(obj => obj.StartTimerWithSuccessTag(name: null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartTimerWithSuccessTagAndEmptyNameShouldThrow()
    {
        this.Invoking(obj => obj.StartTimerWithSuccessTag(name: ""))
            .Should().Throw<ArgumentException>()
            .Which.Message.Should()
            .ContainEquivalentOf("name cannot be empty");
    }

    [Fact]
    public void StartTimerWithSuccessTagAndNullTagsShouldThrow()
    {
        this.Invoking(obj => obj.StartTimerWithSuccessTag(name: "timer", tags: null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartTimerWithSuccessTagAndSingleNullTagShouldThrow()
    {
        this.Invoking(obj => obj.StartTimerWithSuccessTag(name: "timer", tags: [null!]))
            .Should().Throw<ArgumentException>()
            .Which.Message.Should()
            .ContainEquivalentOf("tag cannot be null");
    }

    [Fact]
    public async Task StartTimerWithSuccessTagMetricShouldHaveSuccessTagTrue()
    {
        var delay = TimeSpan.FromSeconds(1);

        using (StartTimerWithSuccessTag("timer", "tag1", "tag2"))
        {
            await Task.Delay(delay, TestContext.Current.CancellationToken);
        }

        _providedTimerName.Should().Be("timer");
        _providedTags.Should().BeEquivalentTo("success:true", "tag1", "tag2");
        _providedElapsedTime.Should().BeCloseTo(delay, precision: TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void StartTimerWithSuccessTagThenTagItAsFailedMetricShouldHaveSuccessTagFalse()
    {
        using (var timer = StartTimerWithSuccessTag("timer"))
        {
            timer.TagAsFailed();
        }

        _providedTags.Should().BeEquivalentTo("success:false");
    }

    [Fact]
    public void StartTimerWithSuccessTagThenTagItAsFailedThenTagItAsSucceededMetricShouldHaveSuccessTagTrue()
    {
        using (var timer = StartTimerWithSuccessTag("timer"))
        {
            timer.TagAsFailed();
            timer.TagAsSucceeded();
        }

        _providedTags.Should().BeEquivalentTo("success:true");
    }

    [Fact]
    public void StartTimerWithSuccessTagThenTagItAsSucceededThenTagItAsFailedMetricShouldHaveSuccessTagFalse()
    {
        using (var timer = StartTimerWithSuccessTag("timer"))
        {
            timer.TagAsSucceeded();
            timer.TagAsFailed();
        }

        _providedTags.Should().BeEquivalentTo("success:false");
    }

    [Fact]
    public void StartTimerWithoutTagThenTagItAsSucceededMetricShouldHaveSuccessTagTrue()
    {
        using (var timer = StartTimer("timer") as MeasurerTimer)
        {
            timer.TagAsSucceeded();
        }

        _providedTags.Should().BeEquivalentTo("success:true");
    }

    [Fact]
    public void StartTimerWithoutTagThenTagItAsFailedMetricShouldHaveSuccessTagFalse()
    {
        using (var timer = StartTimer("timer") as MeasurerTimer)
        {
            timer.TagAsFailed();
        }

        _providedTags.Should().BeEquivalentTo("success:false");
    }

    [Fact]
    public void StartTimerWithTagAndSuccessTagIsNotTheFirstThenTagItAsSucceededMetricShouldHaveSuccessTagTrue()
    {
        using (var timer = StartTimer("timer", "tag1", "tag2", "success:false") as MeasurerTimer)
        {
            timer.TagAsSucceeded();
        }

        // The success tag is the third tag in the list
        _providedTags[2].Should().Be("success:true");
    }

    [Fact]
    public void StartTimerWithTagAndSuccessTagIsNotTheFirstThenTagItAsFailedMetricShouldHaveSuccessTagFalse()
    {
        using (var timer = StartTimer("timer", "tag1", "success:true", "tag2") as MeasurerTimer)
        {
            timer.TagAsFailed();
        }

        // The success tag is the second tag in the list
        _providedTags[1].Should().Be("success:false");
    }

    public override void IncrementCounter(string name, params string[] tags)
    {
    }

    public override void IncrementCounter(string name, int amount, params string[] tags)
    {
    }

    public override void Gauge(string name, double value, params string[] tags)
    {
    }

    public override void Histogram(string name, double value, params string[] tags)
    {
    }

    public override void Timer(string name, TimeSpan elapsedTime, params string[] tags)
    {
        _timerCallCount++;
        _providedTimerName = name;
        _providedElapsedTime = elapsedTime;
        _providedTags = tags;
    }

    public override void Event(string title, string text, params string[] tags)
    {
    }

    private int _timerCallCount;
    private string _providedTimerName = string.Empty;
    private TimeSpan? _providedElapsedTime;
    private string[] _providedTags = [];
}
