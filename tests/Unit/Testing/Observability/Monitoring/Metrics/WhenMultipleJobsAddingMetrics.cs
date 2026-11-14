using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Metrics;

public class WhenMultipleJobsAddingMetricsTests
{
    private const int TaskCount = 100;
    private const int MinMetricId = 1;
    private const int MaxMetricId = 1000;

    [Fact]
    public void GivenMultipleJobsUpdatingCounters_CountersAreAdded()
    {
        var metrics = new InMemoryMeasurer();

        // try to have all jobs start as close to each other as possible.
        // the participant count is _taskCount + 1 because the test itself is also a participant
        var barrier = new Barrier(participantCount: TaskCount + 1);

        var threads = Enumerable.Range(1, TaskCount).Select(_ => StartThread())
                                .ToArray();

        barrier.SignalAndWait(TestContext.Current.CancellationToken);

        foreach (var thread in threads)
            thread.Join();

        foreach (var metricId in Enumerable.Range(MinMetricId, MaxMetricId))
            metrics.Should().HaveCounter(metricId.ToString())
                   .Which.Subject.Tags.Should().ContainSingle()
                   .Which.Samples.Should().HaveCount(TaskCount);

        Thread StartThread()
        {
            var thread = new Thread(_ => AddMetrics(barrier, metrics)) {
                IsBackground = true,
            };
            thread.Start();
            return thread;
        }
    }

    private static void AddMetrics(Barrier barrier, global::Anarchy.Toolkit.Core.Observability.Service.Observability.MeasurerBase metrics)
    {
        barrier.SignalAndWait();

        foreach (var metricId in Enumerable.Range(MinMetricId, MaxMetricId))
            metrics.IncrementCounter(metricId.ToString());
    }
}
