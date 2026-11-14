using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Metrics;

public class WhenPushingMetrics
{
    private const string CounterMetricName = "metric.counter";
    private const string GaugeMetricName = "metric.gauge";
    private const string EventMetricName = "metric.event";
    private const string DefaultTag = "";
    private const string ExampleTag = "foo:bar";

    [Fact]
    public void GivenACounterIsIncremented_CountersCollectionContainsCounter()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == CounterMetricName);
    }

    [Fact]
    public void GivenACounterIsIncrementedAndThenMetricsDataCleared_CountersCollectionEmpty()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);
        metrics.ClearData();

        metrics.GetMetrics().Should().BeEmpty();
    }

    [Fact]
    public void GivenACounterIsIncremented_CountersCollectionContainsDefaultTag()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        metrics.GetMetrics()
               .Single(m => m.Name == CounterMetricName)
               .Tags
               .Should()
               .Contain(tag => tag.Name == DefaultTag);
    }

    [Fact]
    public void GivenACounterIsIncremented_ValueForDefaultTagIsOne()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);

        metrics.GetMetrics()
               .Single(m => m.Name == CounterMetricName)
               .Tags
               .Single(tag => tag.Name == DefaultTag)
               .Samples
               .Last()
               .Should()
               .Be(1.0);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithSpecificTag_ValueForTagIsOne()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName, ExampleTag);

        metrics.GetMetrics()
               .Single(m => m.Name == CounterMetricName)
               .Tags
               .Single(tag => tag.Name == ExampleTag)
               .Samples
               .Last()
               .Should()
               .Be(1.0);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithSpecificTag_ValueForDefaultTagIsOne()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName, ExampleTag);

        metrics.GetMetrics()
               .Single(m => m.Name == CounterMetricName)
               .Tags
               .Single(tag => tag.Name == DefaultTag)
               .Samples
               .Last()
               .Should()
               .Be(1.0);
    }

    [Fact]
    public void GivenACounterIsIncrementedWithSpecificTagAndOnceWithoutTag_ValueForDefaultTagIsTwo()
    {
        var metrics = new InMemoryMeasurer();

        metrics.IncrementCounter(CounterMetricName);
        metrics.IncrementCounter(CounterMetricName, ExampleTag);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == CounterMetricName)
               .Which.Tags.Should().ContainSingle(tag => tag.Name == DefaultTag)
               .Which.Samples.Should().ContainInOrder(1.0, 1.0);
    }

    [Fact]
    public void GivenAGaugeIsPushed_MetricsContainGauge()
    {
        var metrics = new InMemoryMeasurer();
        var gaugeValue = 123.45;

        metrics.Gauge(GaugeMetricName, gaugeValue);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == GaugeMetricName);
    }

    [Fact]
    public void GivenAGaugeIsPushedAndThenMetricsDataCleared_MetricsEmpty()
    {
        var metrics = new InMemoryMeasurer();
        var gaugeValue = 123.45;

        metrics.Gauge(GaugeMetricName, gaugeValue);
        metrics.ClearData();

        metrics.GetMetrics().Should().BeEmpty();
    }

    [Fact]
    public void GivenAEventIsPushed_MetricsContainEvent()
    {
        var metrics = new InMemoryMeasurer();
        var textValue = Guid.NewGuid().ToString();

        metrics.Event(EventMetricName, textValue);

        metrics.GetMetrics().Should().ContainSingle(m => m.Name == EventMetricName);
    }

    [Fact]
    public void GivenAEventIsPushedAndThenMetricsDataCleared_MetricsEmpty()
    {
        var metrics = new InMemoryMeasurer();
        var textValue = Guid.NewGuid().ToString();

        metrics.Event(EventMetricName, textValue);
        metrics.ClearData();

        metrics.GetMetrics().Should().BeEmpty();
    }
}
