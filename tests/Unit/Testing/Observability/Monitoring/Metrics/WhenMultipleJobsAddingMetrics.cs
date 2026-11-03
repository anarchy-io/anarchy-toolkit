using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Metrics;

public class WhenMultipleJobsAddingMetricsTests
{
    private const int _taskCount = 100;
    private const int _minMetricId = 1;
    private const int _maxMetricId = 1000;

    [Fact]
    public void GivenMultipleJobsUpdatingCounters_CountersAreAdded()
    {
        var metrics = new InMemoryMeasurer();

        // try to have all jobs start as close to each other as possible.
        // the participant count is _taskCount + 1 because the test itself is also a participant
        var barrier = new Barrier(participantCount: _taskCount + 1);

        var threads = Enumerable.Range(1, _taskCount).Select(_ => StartThread())
                                .ToArray();

        barrier.SignalAndWait(TestContext.Current.CancellationToken);

        foreach (var thread in threads)
            thread.Join();

        foreach (var metricId in Enumerable.Range(_minMetricId, _maxMetricId))
            metrics.Should().HaveCounter(metricId.ToString())
                   .Which.Subject.Tags.Should().ContainSingle()
                   .Which.Samples.Should().HaveCount(_taskCount);

        Thread StartThread()
        {
            var thread = new Thread(_ => AddMetrics(barrier, metrics)) {
                IsBackground = true,
            };
            thread.Start();
            return thread;
        }
    }

    private void AddMetrics(Barrier barrier, global::Anarchy.Toolkit.Core.Observability.Service.Observability.MeasurerBase metrics)
    {
        barrier.SignalAndWait();

        foreach (var metricId in Enumerable.Range(_minMetricId, _maxMetricId))
            metrics.IncrementCounter(metricId.ToString());
    }
}
