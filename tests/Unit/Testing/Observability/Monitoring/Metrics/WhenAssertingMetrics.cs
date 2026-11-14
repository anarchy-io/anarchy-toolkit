using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using AwesomeAssertions;
using Xunit.Sdk;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Metrics;

public class WhenAssertingMetrics
{
    private const string CounterMetricName = "metric.counter";
    private const string GaugeMetricName = "metric.gauge";
    private const string EventMetricName = "metric.event";
    private const string TimerMetricName = "metric.timer";
    private const string DefaultTag = "";
    private const string ExampleTag = "foo:bar";

    [Fact]
    public void GivenCounterIsIncremented_NotHaveCounterFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        Action action = () => metrics
                             .Should()
                             .NotHaveCounter(CounterMetricName);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metrics to not contain a counter '{CounterMetricName}', but it exists.");
    }

    [Fact]
    public void GivenCounterDoesNotExist_NotHaveCounterSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        Action action = () => metrics.Should()
                                     .NotHaveCounter(CounterMetricName);

        action.Should().NotThrow();
    }

    [Fact]
    public void GivenCounterIsIncremented_HaveCounterSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        metrics.Should()
               .HaveCounter(CounterMetricName);
    }

    [Fact]
    public void GivenCounterDoesNotExist_HaveCounterFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        Action action = () => metrics.Should()
                                     .HaveCounter(CounterMetricName);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metrics to contain a counter '{CounterMetricName}', but it does not exist.");
    }

    [Fact]
    public void GivenACounterIsIncremented_HasTagSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        metrics.Should()
               .HaveCounter(CounterMetricName)
               .Which
               .HasTag(DefaultTag);
    }

    [Fact]
    public void GivenHasTagForNonExistingTag_HasTagFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        Action action = () => metrics.Should()
                                     .HaveCounter(CounterMetricName)
                                     .Which
                                     .HasTag("THIS ONE DOESN'T EXIST");

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected \'{CounterMetricName}\' to have tag 'THIS ONE DOESN'T EXIST', but it does not exist.");
    }

    [Fact]
    public void GivenMetricWithTagIsOne_HasValueFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName, ExampleTag);

        Action action = () => metrics.Should()
                                     .HaveCounter(CounterMetricName)
                                     .Which
                                     .HasTag(ExampleTag)
                                     .And
                                     .HasValue(2);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected \'{CounterMetricName}[{ExampleTag}]\' to have value '2', but found '1'.");
    }

    [Fact]
    public void GivenMetricWithTagIsOne_HasValueForDefaultTagFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName, ExampleTag);

        Action action = () => metrics.Should()
                                     .HaveCounter(CounterMetricName)
                                     .Which
                                     .HasValue(2);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected \'{CounterMetricName}\' to have value '2', but found '1'.");
    }

    [Fact]
    public void GivenACounterIsIncremented_HasTagsSucceed()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        metrics.Should().HaveCounter(CounterMetricName)
               .Which.HasTags([DefaultTag]);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithMultipleTags_HasTagsSucceed()
    {
        var extraTag = $"tag:{Guid.NewGuid():N}";
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName, ExampleTag, extraTag);

        metrics.Should().HaveCounter(CounterMetricName)
               .Which.HasTags([DefaultTag, ExampleTag, extraTag]);
    }

    [Fact]
    public void GivenHasTagsForNonExistingTag_HasTagsFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();
        string[] tags = ["test:tag", "test:tag2"];
        string[] expectedTags = ["test:tag", "THIS ONE DOESN'T EXIST", "test:tag2"];

        metrics.IncrementCounter(CounterMetricName, tags);

        Action action = () => metrics.Should().HaveCounter(CounterMetricName)
                                     .Which.HasTags(expectedTags);
        action.Should().Throw<XunitException>()
              .Which.Message.Should().Match(
                   $"Expected '{CounterMetricName}' to have tags [{string.Join(", ", expectedTags)}]*"
               );
    }

    [Fact]
    public void GivenGaugeIsPushed_HaveGaugeSucceeds()
    {
        const double someMetricValue = 123.45;

        var metrics = new InMemoryMeasurer();

        metrics.Gauge(GaugeMetricName, someMetricValue);

        metrics.Should()
               .HaveGauge(GaugeMetricName)
               .Which.HasValue(someMetricValue);
    }

    [Fact]
    public void GivenGaugeMetricIsPushed_AndMeasurerScopeExists_AddsMeasurerScopeTagToMetric()
    {
        const string someMetricName = "some.gauge";
        const string scopeTag = "scope:tag";
        const double someMetricValue = 123.45;
        var metrics = new InMemoryMeasurer();

        using var _ = MeasurerBase.BeginScope(scopeTag);

        metrics.Gauge(someMetricName, someMetricValue);

        metrics.Should().HaveGauge(someMetricName)
               .Which.HasTag(scopeTag);
    }

    [Fact]
    public void GivenNoGaugeIsPushed_NotHaveGaugeSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.Should()
            .NotHaveGauge(GaugeMetricName);
    }

    [Fact]
    public void GivenEventIsPushed_HaveEventSucceeds()
    {
        const string someMetricValue = "Anarchy";

        var metrics = new InMemoryMeasurer();

        metrics.Event(EventMetricName, someMetricValue);

        metrics.Should()
               .HaveEvent(EventMetricName)
               .Which.HasValue(someMetricValue);
    }

    [Fact]
    public void GiveEventIsPushed_AndMeasurerScopeExists_AddsMeasurerScopeTagToMetric()
    {
        const string someMetricName = "some.event";
        const string scopeTag = "scope:tag";
        const string someMetricValue = "iddqd";
        var metrics = new InMemoryMeasurer();

        using var _ = MeasurerBase.BeginScope(scopeTag);

        metrics.Event(someMetricName, someMetricValue);

        metrics.Should().HaveEvent(someMetricName)
               .Which.HasTag(scopeTag);
    }

    [Fact]
    public void GivenNoEventIsPushed_NotHaveEventSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.Should()
            .NotHaveEvent(EventMetricName);
    }

    [Fact]
    public void GivenTwoSamplesAndExpectingThree_HasSamplesFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);
        metrics.IncrementCounter(CounterMetricName);

        Action action = () => metrics.Should()
                                     .HaveCounter(CounterMetricName)
                                     .Which
                                     .HasSamples([1.0, 1.0, 1.0]);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metric '{CounterMetricName}' to have 3 samples but found 2");
    }

    [Fact]
    public void GivenThreeSamplesAndLastDoesNotMatch_HasSamplesFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);
        metrics.IncrementCounter(CounterMetricName);
        metrics.IncrementCounter(CounterMetricName);

        Action action = () => metrics.Should()
                                     .HaveCounter(CounterMetricName)
                                     .Which
                                     .HasSamples([1.0, 1.0, 2.0]);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metric \"{CounterMetricName}\" to contain samples {{1.0, 1.0, 2.0}} in order, but 2.0 (index 2) did not appear (in the right order).");
    }

    [Fact]
    public void GivenThreeSamplesWithTag_HasSamplesSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName, ExampleTag);
        metrics.IncrementCounter(CounterMetricName, ExampleTag);
        metrics.IncrementCounter(CounterMetricName);

        metrics.Should()
               .HaveCounter(CounterMetricName)
               .Which
               .HasTag(ExampleTag)
               .And
               .HasSamples([1.0, 1.0]);
    }

    [Fact]
    public void GivenIncrementCounterMetricIsPushed_AndMeasurerScopeExists_AddsMeasurerScopeTagToMetric()
    {
        const string someMetricName = "some.count";
        const string scopeTag = "scope:tag";
        var metrics = new InMemoryMeasurer();

        using var _ = MeasurerBase.BeginScope(scopeTag);

        metrics.IncrementCounter(someMetricName);

        metrics.Should().HaveCounter(someMetricName)
               .Which.HasTag(scopeTag);
    }

    [Fact]
    public void GivenTimerMetricWithValue_HasValueReturnsValue()
    {
        var metrics = new InMemoryMeasurer();
        var expectedValue = TimeSpan.FromMinutes(42);
        metrics.Timer(TimerMetricName, expectedValue);

        var actual =
            metrics.Should().HaveTimer(TimerMetricName).Which.HasValue();

        actual.Which.Should().Be(expectedValue);
    }

    [Fact]
    public void GivenTimerMetricIsPushed_AndMeasurerScopeExists_AddsMeasurerScopeTagToMetric()
    {
        const string someMetricName = "some.timer";
        const string scopeTag = "scope:tag";
        var timeSpan = TimeSpan.FromMinutes(42);

        var metrics = new InMemoryMeasurer();

        using var _ = MeasurerBase.BeginScope(scopeTag);

        metrics.Timer(someMetricName, timeSpan);

        metrics.Should().HaveTimer(someMetricName)
               .Which.HasTag(scopeTag);
    }

    [Fact]
    public void GivenNoMetric_HaveTimerFails()
    {
        var metrics = new InMemoryMeasurer();

        metrics.Invoking(m => m.Should().HaveTimer(TimerMetricName))
               .Should().Throw<XunitException>()
               .WithMessage($"Expected metrics to contain a timer '{TimerMetricName}', but it does not exist.");
    }

    [Fact]
    public void GivenNoTimerMetric_NotHaveTimerSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.Should().NotHaveTimer(TimerMetricName);
    }

    [Fact]
    public void GivenHistogramMetricIsPushed_HaveHistogramSucceeds()
    {
        const string someMetricName = "some.histogram";
        const double someMetricValue = 1.1;

        var metrics = new InMemoryMeasurer();

        metrics.Histogram(someMetricName, someMetricValue);

        metrics.Should().HaveHistogram(someMetricName)
            .Which.HasValue(someMetricValue);
    }

    [Fact]
    public void GivenHistogramMetricIsPushed_AndMeasurerScopeExists_AddsMeasurerScopeTagToMetric()
    {
        const string someMetricName = "some.histogram";
        const string scopeTag = "scope:tag";
        const double someMetricValue = 1.1;

        var metrics = new InMemoryMeasurer();

        using var _ = MeasurerBase.BeginScope(scopeTag);

        metrics.Histogram(someMetricName, someMetricValue);

        metrics.Should().HaveHistogram(someMetricName)
               .Which.HasTag(scopeTag);
    }

    [Fact]
    public void GivenNoHistogramMetric_NotHaveHistogramSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.Should().NotHaveHistogram(TimerMetricName);
    }
}
