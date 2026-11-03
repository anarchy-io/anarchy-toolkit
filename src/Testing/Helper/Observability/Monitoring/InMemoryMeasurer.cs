using Anarchy.Toolkit.Core.Observability.Service.Observability;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring
{
    /// <summary>
    /// Implementation of metrics with memory backed storage
    /// </summary>
    /// <inheritdoc />
    public class InMemoryMeasurer : MeasurerBase
    {
        internal const string MetricTypeCounter = "counter";
        internal const string MetricTypeGauge = "gauge";
        internal const string MetricTypeHistogram = "histogram";
        internal const string MetricTypeTimer = "timer";
        internal const string MetricTypeEvent = "event";

        internal List<Metric> Metrics => _metrics.Values.ToList();
        private readonly ConcurrentDictionary<(string, string), Metric> _metrics = new();

        /// <inheritdoc />
        public override void IncrementCounter(string name, params string[] tags) =>
            IncrementCounter(name, amount: 1, tags);

        /// <inheritdoc />
        public override void IncrementCounter(string name, int amount, params string[] tags) =>
            Store<double>(name, amount, MetricTypeCounter, GetTags(tags));

        /// <inheritdoc />
        public override void Gauge(string name, double value, params string[] tags) =>
            Store(name, value, MetricTypeGauge, GetTags(tags));

        /// <inheritdoc />
        public override void Histogram(string name, double value, params string[] tags) =>
            Store(name, value, MetricTypeHistogram, GetTags(tags));

        /// <inheritdoc />
        public override void Timer(string name, TimeSpan elapsedTime, params string[] tags) =>
            Store(name, elapsedTime, MetricTypeTimer, GetTags(tags));

        /// <inheritdoc />
        public override void Event(string title, string text, params string[] tags) =>
            Store(title, text, MetricTypeEvent, GetTags(tags));

        /// <summary>
        /// Clears all recorded samples.
        /// </summary>
        public void ClearData()
        {
            _metrics.Clear();
        }

        private void Store<TValue>(
            string name,
            TValue value,
            string type,
            params string[] tags
        ) where TValue : notnull
        {
            var metric = _metrics.GetOrAdd(
                (name, type),
                _ => new Metric {
                    Name = name,
                    Type = type,
                }
            );

            foreach (var tag in tags.Union([string.Empty]))
            {
                var instance = metric.GetOrAddTag(tag);
                instance.AddSample(value);
            }
        }
    }
}
