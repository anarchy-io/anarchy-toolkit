using Anarchy.Toolkit.Core.Observability.Entity.Service;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

public partial class Observer
{
    #region ------------- MeasurerBase.Timer --------------

    public void ExecutionRecordDuration(string prefix, TimeSpan elapsed, params string[] tags)
    {
        MeasurerBase.Timer($"{prefix}.{MetricSuffixExecTime}", elapsed, tags);
    }

    public void ExecutionRecordDuration(CallSiteEntity callSite, TimeSpan elapsed, params string[] tags)
    {
        ExecutionRecordDuration(callSite.ToMetricPrefix(), elapsed, tags);
    }

    public void RecordDuration(string metricName, TimeSpan elapsed, params string[] tags)
    {
        MeasurerBase.Timer(metricName, elapsed, tags);
    }

    public void RecordDuration(CallSiteEntity callSite, string suffix, TimeSpan elapsed, params string[] tags)
    {
        RecordDuration($"{callSite.ToMetricPrefix()}.{suffix}", elapsed, tags);
    }

    public void RecordDuration(CallSiteEntity callSite, TimeSpan elapsed, params string[] tags)
    {
        RecordDuration(callSite, MetricSuffixDuration, elapsed, tags);
    }

    #endregion

    #region ------------- MeasurerBase.IncrementCounter --------------

    public void ExecutionIncrementCount(string prefix, int count = 1, params string[] tags)
    {
        MeasurerBase.IncrementCounter($"{prefix}.{MetricSuffixExecCount}", count, tags);
    }

    public void ExecutionIncrementCount(CallSiteEntity callSite, int count = 1, params string[] tags)
    {
        ExecutionIncrementCount(callSite.ToMetricPrefix(), count, tags);
    }

    public void IncrementCounter(string metric, int count = 1, params string[] tags)
    {
        MeasurerBase.IncrementCounter(metric, count, tags);
    }

    public void IncrementCounter(CallSiteEntity callSite, string suffix, int count = 1, params string[] tags)
    {
        IncrementCounter($"{callSite.ToMetricPrefix()}.{suffix}", count, tags);
    }

    #endregion

    #region ------------- MeasurerBase.Gauge --------------

    public void Gauge(string metric, int count = 1, params string[] tags)
    {
        MeasurerBase.Gauge(metric, count, tags);
    }

    public void Gauge(CallSiteEntity callSite, string suffix, int count = 1, params string[] tags)
    {
        Gauge($"{callSite.ToMetricPrefix()}.{suffix}", count, tags);
    }

    #endregion
}
