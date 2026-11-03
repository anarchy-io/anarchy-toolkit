using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring
{
    /// <summary>
    /// Represents a metric
    /// </summary>
    public class Metric
    {
        /// <summary>
        /// Name of the metric
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Type of the metric (counter, gauge, histogram or timer)
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Tags on this metric
        /// </summary>
        public List<Tag> Tags => _tags.Values.ToList();

        internal Tag GetOrAddTag(string name) => _tags.GetOrAdd(name, n => new Tag { Name = n });

        private readonly ConcurrentDictionary<string, Tag> _tags = new ConcurrentDictionary<string, Tag>();
    }
}
