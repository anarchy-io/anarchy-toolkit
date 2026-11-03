using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using AwesomeAssertions;
using Xunit.Sdk;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Metrics;

public class WhenAssertingMetrics
{
    private const string _counterMetricName = "metric.counter";
    private const string _gaugeMetricName = "metric.gauge";
    private const string _eventMetricName = "metric.event";
    private const string _timerMetricName = "metric.timer";
    private const string _defaultTag = "";
    private const string _exampleTag = "foo:bar";

    [Fact]
    public void GivenCounterIsIncremented_NotHaveCounterFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        Action action = () => metrics
                             .Should()
                             .NotHaveCounter(_counterMetricName);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metrics to not contain a counter '{_counterMetricName}', but it exists.");
    }

    [Fact]
    public void GivenCounterDoesNotExist_NotHaveCounterSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        Action action = () => metrics.Should()
                                     .NotHaveCounter(_counterMetricName);

        action.Should().NotThrow();
    }

    [Fact]
    public void GivenCounterIsIncremented_HaveCounterSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        metrics.Should()
               .HaveCounter(_counterMetricName);
    }

    [Fact]
    public void GivenCounterDoesNotExist_HaveCounterFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        Action action = () => metrics.Should()
                                     .HaveCounter(_counterMetricName);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metrics to contain a counter '{_counterMetricName}', but it does not exist.");
    }

    [Fact]
    public void GivenACounterIsIncremented_HasTagSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        metrics.Should()
               .HaveCounter(_counterMetricName)
               .Which
               .HasTag(_defaultTag);
    }

    [Fact]
    public void GivenHasTagForNonExistingTag_HasTagFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        Action action = () => metrics.Should()
                                     .HaveCounter(_counterMetricName)
                                     .Which
                                     .HasTag("THIS ONE DOESN'T EXIST");

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected \'{_counterMetricName}\' to have tag 'THIS ONE DOESN'T EXIST', but it does not exist.");
    }

    [Fact]
    public void GivenMetricWithTagIsOne_HasValueFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName, _exampleTag);

        Action action = () => metrics.Should()
                                     .HaveCounter(_counterMetricName)
                                     .Which
                                     .HasTag(_exampleTag)
                                     .And
                                     .HasValue(2);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected \'{_counterMetricName}[{_exampleTag}]\' to have value '2', but found '1'.");
    }

    [Fact]
    public void GivenMetricWithTagIsOne_HasValueForDefaultTagFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName, _exampleTag);

        Action action = () => metrics.Should()
                                     .HaveCounter(_counterMetricName)
                                     .Which
                                     .HasValue(2);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected \'{_counterMetricName}\' to have value '2', but found '1'.");
    }

    [Fact]
    public void GivenACounterIsIncremented_HasTagsSucceed()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        metrics.Should().HaveCounter(_counterMetricName)
               .Which.HasTags([_defaultTag]);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithMultipleTags_HasTagsSucceed()
    {
        var extraTag = $"tag:{Guid.NewGuid():N}";
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName, _exampleTag, extraTag);

        metrics.Should().HaveCounter(_counterMetricName)
               .Which.HasTags([_defaultTag, _exampleTag, extraTag]);
    }

    [Fact]
    public void GivenHasTagsForNonExistingTag_HasTagsFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();
        string[] tags = ["test:tag", "test:tag2"];
        string[] expectedTags = ["test:tag", "THIS ONE DOESN'T EXIST", "test:tag2"];

        metrics.IncrementCounter(_counterMetricName, tags);

        Action action = () => metrics.Should().HaveCounter(_counterMetricName)
                                     .Which.HasTags(expectedTags);
        action.Should().Throw<XunitException>()
              .Which.Message.Should().Match(
                   $"Expected '{_counterMetricName}' to have tags [{string.Join(", ", expectedTags)}]*"
               );
    }

    [Fact]
    public void GivenGaugeIsPushed_HaveGaugeSucceeds()
    {
        const double someMetricValue = 123.45;

        var metrics = new InMemoryMeasurer();

        metrics.Gauge(_gaugeMetricName, someMetricValue);

        metrics.Should()
               .HaveGauge(_gaugeMetricName)
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
            .NotHaveGauge(_gaugeMetricName);
    }

    [Fact]
    public void GivenEventIsPushed_HaveEventSucceeds()
    {
        const string someMetricValue = "Anarchy";

        var metrics = new InMemoryMeasurer();

        metrics.Event(_eventMetricName, someMetricValue);

        metrics.Should()
               .HaveEvent(_eventMetricName)
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
            .NotHaveEvent(_eventMetricName);
    }

    [Fact]
    public void GivenTwoSamplesAndExpectingThree_HasSamplesFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);
        metrics.IncrementCounter(_counterMetricName);

        Action action = () => metrics.Should()
                                     .HaveCounter(_counterMetricName)
                                     .Which
                                     .HasSamples([1.0, 1.0, 1.0]);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metric '{_counterMetricName}' to have 3 samples but found 2");
    }

    [Fact]
    public void GivenThreeSamplesAndLastDoesNotMatch_HasSamplesFailsWithMessage()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);
        metrics.IncrementCounter(_counterMetricName);
        metrics.IncrementCounter(_counterMetricName);

        Action action = () => metrics.Should()
                                     .HaveCounter(_counterMetricName)
                                     .Which
                                     .HasSamples([1.0, 1.0, 2.0]);

        action.Should().Throw<XunitException>()
              .Which
              .Message
              .Should()
              .Be($"Expected metric \"{_counterMetricName}\" to contain samples {{1.0, 1.0, 2.0}} in order, but 2.0 (index 2) did not appear (in the right order).");
    }

    [Fact]
    public void GivenThreeSamplesWithTag_HasSamplesSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName, _exampleTag);
        metrics.IncrementCounter(_counterMetricName, _exampleTag);
        metrics.IncrementCounter(_counterMetricName);

        metrics.Should()
               .HaveCounter(_counterMetricName)
               .Which
               .HasTag(_exampleTag)
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
        metrics.Timer(_timerMetricName, expectedValue);

        var actual =
            metrics.Should().HaveTimer(_timerMetricName).Which.HasValue();

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

        metrics.Invoking(m => m.Should().HaveTimer(_timerMetricName))
               .Should().Throw<XunitException>()
               .WithMessage($"Expected metrics to contain a timer '{_timerMetricName}', but it does not exist.");
    }

    [Fact]
    public void GivenNoTimerMetric_NotHaveTimerSucceeds()
    {
        var metrics = new InMemoryMeasurer();

        metrics.Should().NotHaveTimer(_timerMetricName);
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

        metrics.Should().NotHaveHistogram(_timerMetricName);
    }
}
