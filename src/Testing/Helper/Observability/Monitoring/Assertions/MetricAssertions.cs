using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions
{
    /// <inheritdoc />
    public class MetricAssertions<TValue> : ReferenceTypeAssertions<Metric, MetricAssertions<TValue>>
    {
        private readonly AssertionChain _assertionChain;

        /// <inheritdoc />
        public MetricAssertions(Metric subject, string name, AssertionChain assertionChain) : base(subject, assertionChain)
        {
            Identifier = name;
            _assertionChain = assertionChain;
        }

        /// <inheritdoc />
        protected override string Identifier { get; }

        /// <summary>
        /// Asserts that the metric has a given tag
        /// </summary>
        /// <param name="tag">The name of the tag</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<TaggedMetricAssertions<TValue>, Tag> HasTag(
            string tag,
            string because = "",
            params object[] becauseArgs
        )
        {
            var matchedTag = Subject.Tags.SingleOrDefault(t => t.Name == tag);

            _assertionChain
                   .BecauseOf(because, becauseArgs)
                   .ForCondition(matchedTag != null)
                   .FailWith($"Expected '{Identifier}' to have tag '{tag}', but it does not exist.");

            var metricAndTag = tag == "" ? Identifier : $"{Identifier}[{tag}]";

            return new AndWhichConstraint<TaggedMetricAssertions<TValue>, Tag>(
                new TaggedMetricAssertions<TValue>(matchedTag!, metricAndTag, _assertionChain),
                matchedTag!
            );
        }

        /// <summary>
        /// Asserts that the metric has a given tag
        /// </summary>
        /// <param name="tags">The list of tag names</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndConstraint<MetricAssertions<TValue>> HasTags(
            string[] tags,
            string because = "",
            params object[] becauseArgs
        )
        {
            _assertionChain
                   .BecauseOf(because, becauseArgs)
                   .ForCondition(tags.All(tag => Subject.Tags.Any(t => t.Name == tag)))
                   .FailWith($"Expected '{Identifier}' to have tags [{string.Join(", ", tags)}], but found [{string.Join(", ", Subject.Tags.Select(t => t.Name))}].");

            return new AndConstraint<MetricAssertions<TValue>>(this);
        }

        /// <summary>
        /// Asserts that the last sample of the metric has a given value
        /// </summary>
        /// <remarks>This only verifies the last pushed metric. For all samples of the metric <see cref="HasSamples"/></remarks>
        /// <param name="value">The expected value</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndConstraint<TValue> HasValue(
            TValue value,
            string because = "",
            params object[] becauseArgs
        ) =>
            HasTag("").And.HasValue(value, because, becauseArgs);

        /// <summary>
        /// Asserts that the last sample of the metric has a value
        /// </summary>
        /// <remarks>This only returns the last pushed metric value. For all samples of the metric <see cref="HasSamples"/></remarks>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<TaggedMetricAssertions<TValue>, TValue> HasValue(
            string because = "",
            params object[] becauseArgs
        )
        {
            var tag = HasTag("").And;

            return new AndWhichConstraint<TaggedMetricAssertions<TValue>, TValue>(
                tag,
                tag.WhoseValue
            );
        }

        /// <summary>
        /// Asserts that the metric contains the samples in the given order.
        /// </summary>
        /// <param name="expected">The expected samples</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndConstraint<IEnumerable<TValue>> HasSamples(
            IEnumerable<TValue> expected,
            string because = "",
            params object[] becauseArgs
        ) =>
            HasTag("").And.HasSamples(expected, because, becauseArgs);
    }
}
