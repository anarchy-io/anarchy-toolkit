using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Metrics;

public class WhenPushingMetrics
{
    private const string _counterMetricName = "metric.counter";
    private const string _gaugeMetricName = "metric.gauge";
    private const string _eventMetricName = "metric.event";
    private const string _defaultTag = "";
    private const string _exampleTag = "foo:bar";

    [Fact]
    public void GivenACounterIsIncremented_CountersCollectionContainsCounter()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == _counterMetricName);
    }

    [Fact]
    public void GivenACounterIsIncrementedAndThenMetricsDataCleared_CountersCollectionEmpty()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);
        metrics.ClearData();

        metrics.GetMetrics().Should().BeEmpty();
    }

    [Fact]
    public void GivenACounterIsIncremented_CountersCollectionContainsDefaultTag()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        metrics.GetMetrics()
               .Single(m => m.Name == _counterMetricName)
               .Tags
               .Should()
               .Contain(tag => tag.Name == _defaultTag);
    }

    [Fact]
    public void GivenACounterIsIncremented_ValueForDefaultTagIsOne()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);

        metrics.GetMetrics()
               .Single(m => m.Name == _counterMetricName)
               .Tags
               .Single(tag => tag.Name == _defaultTag)
               .Samples
               .Last()
               .Should()
               .Be(1.0);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithSpecificTag_ValueForTagIsOne()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName, _exampleTag);

        metrics.GetMetrics()
               .Single(m => m.Name == _counterMetricName)
               .Tags
               .Single(tag => tag.Name == _exampleTag)
               .Samples
               .Last()
               .Should()
               .Be(1.0);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithSpecificTag_ValueForDefaultTagIsOne()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName, _exampleTag);

        metrics.GetMetrics()
               .Single(m => m.Name == _counterMetricName)
               .Tags
               .Single(tag => tag.Name == _defaultTag)
               .Samples
               .Last()
               .Should()
               .Be(1.0);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithSpecificTagAndOnceWithoutTag_ValueForDefaultTagIsTwo()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(_counterMetricName);
        metrics.IncrementCounter(_counterMetricName, _exampleTag);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == _counterMetricName)
               .Which.Tags.Should().ContainSingle(tag => tag.Name == _defaultTag)
               .Which.Samples.Should().ContainInOrder(1.0, 1.0);
    }

    [Fact]
    public void GivenAGaugeIsPushed_MetricsContainGauge()
    {
        var metrics = new InMemoryMeasurer();
        var gaugeValue = 123.45;

        metrics.Gauge(_gaugeMetricName, gaugeValue);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == _gaugeMetricName);
    }

    [Fact]
    public void GivenAGaugeIsPushedAndThenMetricsDataCleared_MetricsEmpty()
    {
        var metrics = new InMemoryMeasurer();
        var gaugeValue = 123.45;

        metrics.Gauge(_gaugeMetricName, gaugeValue);
        metrics.ClearData();

        metrics.GetMetrics().Should().BeEmpty();
    }

    [Fact]
    public void GivenAEventIsPushed_MetricsContainEvent()
    {
        var metrics = new InMemoryMeasurer();
        var textValue = Guid.NewGuid().ToString();

        metrics.Event(_eventMetricName, textValue);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == _eventMetricName);
    }

    [Fact]
    public void GivenAEventIsPushedAndThenMetricsDataCleared_MetricsEmpty()
    {
        var metrics = new InMemoryMeasurer();
        var textValue = Guid.NewGuid().ToString();

        metrics.Event(_eventMetricName, textValue);
        metrics.ClearData();

        metrics.GetMetrics().Should().BeEmpty();
    }
}
