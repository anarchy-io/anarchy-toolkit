using Anarchy.Toolkit.Infrastructure.Telemetry.StatsD;
using AwesomeAssertions;
using Moq;
using StatsdClient;
using System.Globalization;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Telemetry.StatsD;

public class StatsDMetricRecorderTest
{
    [Fact]
    public void RecordMetrics_MapsValuesAndTagsWithoutChangingCulture()
    {
        var client = new Mock<IDogStatsd>();
        var recorder = new StatsDMetricRecorder(client.Object);
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            KeyValuePair<string, object?>[] tags = [new("success", true), new("ratio", 1.5), new("missing", null)];
            recorder.IncrementCounter("requests", 3, tags);
            recorder.RecordGauge("depth", 2.5, tags);
            recorder.RecordHistogram("size", 4.5, tags);
            recorder.RecordDuration("duration", TimeSpan.FromMilliseconds(1250), tags);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        client.Verify(c => c.Counter("requests", 3, 1, It.Is<string[]>(t => TagsMatch(t)), null, null), Times.Once);
        client.Verify(c => c.Gauge("depth", 2.5, 1, It.Is<string[]>(t => TagsMatch(t)), null, null), Times.Once);
        client.Verify(c => c.Histogram("size", 4.5, 1, It.Is<string[]>(t => TagsMatch(t)), null), Times.Once);
        client.Verify(c => c.Histogram("duration", 1.25, 1, It.Is<string[]>(t => TagsMatch(t)), null), Times.Once);
    }

    [Fact]
    public void RecordHistogram_WhenDistributionsEnabled_SendsDistributionInSeconds()
    {
        var client = new Mock<IDogStatsd>();
        var recorder = new StatsDMetricRecorder(client.Object, useDistributions: true);
        recorder.RecordHistogram("size", 42);
        recorder.RecordDuration("duration", TimeSpan.FromSeconds(1.5));

        client.Verify(c => c.Distribution("size", 42, 1, It.IsAny<string[]>(), null), Times.Once);
        client.Verify(c => c.Distribution("duration", 1.5, 1, It.IsAny<string[]>(), null), Times.Once);
    }

    [Fact]
    public void RecordGauge_WhenNameIsBlank_Throws()
    {
        var recorder = new StatsDMetricRecorder(Mock.Of<IDogStatsd>());
        var act = () => recorder.RecordGauge(" ", 1);
        act.Should().Throw<ArgumentException>();
    }

    private static bool TagsMatch(string[] tags) => tags.SequenceEqual(new[] { "success:true", "ratio:1.5" });
}
