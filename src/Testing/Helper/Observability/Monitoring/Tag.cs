using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring
{
    /// <summary>
    /// Represents a tag on a <see cref="Metric">metric</see>
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// Name of the tag
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Collected samples on this tag
        /// </summary>
        public List<object> Samples => _samples.ToList();

        internal void AddSample(object sample) => _samples.Add(sample);

        private readonly ConcurrentBag<object> _samples = new ConcurrentBag<object>();
    }
}
