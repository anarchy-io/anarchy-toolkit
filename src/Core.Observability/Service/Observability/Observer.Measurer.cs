using Anarchy.Toolkit.Core.Observability.Entity.Service;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

public partial class Observer
{
    #region ------------- MeasurerBase.Timer --------------

    /// <summary>
    /// Records an execution duration as the timer metric <c>{prefix}.execution_time</c>.
    /// </summary>
    /// <param name="prefix">The metric name prefix.</param>
    /// <param name="elapsed">The measured duration.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format (see <see cref="Tags"/>).</param>
    public void ExecutionRecordDuration(string prefix, TimeSpan elapsed, params string[] tags)
    {
        MeasurerBase.Timer($"{prefix}.{MetricSuffixExecTime}", elapsed, tags);
    }

    /// <summary>
    /// Records an execution duration as the timer metric <c>{class_name}.{method_name}.execution_time</c>.
    /// </summary>
    /// <param name="callSite">The call site used to build the metric prefix (see <see cref="CallSiteEntity.ToMetricPrefix"/>).</param>
    /// <param name="elapsed">The measured duration.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void ExecutionRecordDuration(CallSiteEntity callSite, TimeSpan elapsed, params string[] tags)
    {
        ExecutionRecordDuration(callSite.ToMetricPrefix(), elapsed, tags);
    }

    /// <summary>
    /// Records a duration for the timer metric <paramref name="metricName"/>.
    /// </summary>
    /// <param name="metricName">The full metric name.</param>
    /// <param name="elapsed">The measured duration.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void RecordDuration(string metricName, TimeSpan elapsed, params string[] tags)
    {
        MeasurerBase.Timer(metricName, elapsed, tags);
    }

    /// <summary>
    /// Records a duration for the timer metric <c>{class_name}.{method_name}.{suffix}</c>.
    /// </summary>
    /// <param name="callSite">The call site used to build the metric prefix.</param>
    /// <param name="suffix">The metric name suffix.</param>
    /// <param name="elapsed">The measured duration.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void RecordDuration(CallSiteEntity callSite, string suffix, TimeSpan elapsed, params string[] tags)
    {
        RecordDuration($"{callSite.ToMetricPrefix()}.{suffix}", elapsed, tags);
    }

    /// <summary>
    /// Records a duration for the timer metric <c>{class_name}.{method_name}.duration</c>.
    /// </summary>
    /// <param name="callSite">The call site used to build the metric prefix.</param>
    /// <param name="elapsed">The measured duration.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void RecordDuration(CallSiteEntity callSite, TimeSpan elapsed, params string[] tags)
    {
        RecordDuration(callSite, MetricSuffixDuration, elapsed, tags);
    }

    #endregion

    #region ------------- MeasurerBase.IncrementCounter --------------

    /// <summary>
    /// Increments the counter metric <c>{prefix}.execution_count</c>.
    /// </summary>
    /// <param name="prefix">The metric name prefix.</param>
    /// <param name="count">The amount to increment by. Defaults to 1.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void ExecutionIncrementCount(string prefix, int count = 1, params string[] tags)
    {
        MeasurerBase.IncrementCounter($"{prefix}.{MetricSuffixExecCount}", count, tags);
    }

    /// <summary>
    /// Increments the counter metric <c>{class_name}.{method_name}.execution_count</c>.
    /// </summary>
    /// <param name="callSite">The call site used to build the metric prefix.</param>
    /// <param name="count">The amount to increment by. Defaults to 1.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void ExecutionIncrementCount(CallSiteEntity callSite, int count = 1, params string[] tags)
    {
        ExecutionIncrementCount(callSite.ToMetricPrefix(), count, tags);
    }

    /// <summary>
    /// Increments the counter metric <paramref name="metric"/>.
    /// </summary>
    /// <param name="metric">The full metric name.</param>
    /// <param name="count">The amount to increment by. Defaults to 1.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void IncrementCounter(string metric, int count = 1, params string[] tags)
    {
        MeasurerBase.IncrementCounter(metric, count, tags);
    }

    /// <summary>
    /// Increments the counter metric <c>{class_name}.{method_name}.{suffix}</c>.
    /// </summary>
    /// <param name="callSite">The call site used to build the metric prefix.</param>
    /// <param name="suffix">The metric name suffix (e.g. <see cref="MetricSuffixFailed"/>).</param>
    /// <param name="count">The amount to increment by. Defaults to 1.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void IncrementCounter(CallSiteEntity callSite, string suffix, int count = 1, params string[] tags)
    {
        IncrementCounter($"{callSite.ToMetricPrefix()}.{suffix}", count, tags);
    }

    #endregion

    #region ------------- MeasurerBase.Gauge --------------

    /// <summary>
    /// Sets the gauge metric <paramref name="metric"/> to <paramref name="count"/>.
    /// </summary>
    /// <param name="metric">The full metric name.</param>
    /// <param name="count">The current value. Defaults to 1.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void Gauge(string metric, int count = 1, params string[] tags)
    {
        MeasurerBase.Gauge(metric, count, tags);
    }

    /// <summary>
    /// Sets the gauge metric <c>{class_name}.{method_name}.{suffix}</c> to <paramref name="count"/>.
    /// </summary>
    /// <param name="callSite">The call site used to build the metric prefix.</param>
    /// <param name="suffix">The metric name suffix.</param>
    /// <param name="count">The current value. Defaults to 1.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public void Gauge(CallSiteEntity callSite, string suffix, int count = 1, params string[] tags)
    {
        Gauge($"{callSite.ToMetricPrefix()}.{suffix}", count, tags);
    }

    #endregion
}
