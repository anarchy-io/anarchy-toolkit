using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions
{
    /// <inheritdoc />
    public class TaggedMetricAssertions<TValue> : ReferenceTypeAssertions<Tag, TaggedMetricAssertions<TValue>>
    {
        private readonly AssertionChain _assertionChain;

        /// <inheritdoc />
        public TaggedMetricAssertions(Tag subject, string metricAndTag, AssertionChain assertionChain) : base(subject, assertionChain)
        {
            Identifier = metricAndTag;
            _assertionChain = assertionChain;
        }

        /// <inheritdoc />
        protected override string Identifier { get; }

        /// <summary>
        /// Returns the last pushed value of the metric
        /// </summary>
        public TValue WhoseValue => (TValue)Subject.Samples.Last();

        /// <summary>
        ///     Asserts that the metric has a given expected
        /// </summary>
        /// <param name="expected">The expected expected</param>
        /// <param name="because">
        ///     A formatted phrase explaining why the condition should be satisfied. If the phrase does not start
        ///     with the word because, it is prepended to the message.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more values to use for filling in any
        ///     <see cref="string.Format(string,object[])"/> compatible placeholders.
        /// </param>
        /// <returns></returns>
        public AndConstraint<TValue> HasValue(
            TValue expected,
            string because = "",
            params object[] becauseArgs
        )
        {
            var lastSample = (TValue)Subject.Samples.Last();

            _assertionChain
                   .BecauseOf(because, becauseArgs)
                   .ForCondition(lastSample.Equals(expected))
                   .FailWith($"Expected '{Identifier}' to have value '{expected}', but found '{lastSample}'.");

            return new AndConstraint<TValue>(lastSample);
        }

        /// <summary>
        /// Asserts that the tagged metric contains the samples in the given order.
        /// </summary>
        /// <param name="expected">The expected samples</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndConstraint<IEnumerable<TValue>> HasSamples(
            IEnumerable<TValue> expected,
            string because = "",
            params object[] becauseArgs
        )
        {
            var actualItems = Subject.Samples.Cast<TValue>().ToArray();
            var expectedItems = expected.ToArray();

            _assertionChain
                   .BecauseOf(because, becauseArgs)
                   .ForCondition(expectedItems.Length == actualItems.Length)
                   .FailWith($"Expected metric '{Identifier}' to have {expectedItems.Length} samples but found {actualItems.Length}");

            for (var index = 0; index < expectedItems.Length; index++)
            {
                object? expectedItem = expectedItems[index];
                actualItems = actualItems.SkipWhile(actualItem => !Equals(actualItem, expectedItem)).ToArray();
                if (actualItems.Any())
                {
                    actualItems = actualItems.Skip(1).ToArray();
                }
                else
                {
                    _assertionChain
                           .BecauseOf(because, becauseArgs)
                           .ForCondition(false)
                           .FailWith(
                                "Expected metric {0} to contain samples {1} in order{reason}, but {2} (index {3}) did not appear (in the right order).",
                                Identifier,
                                expected,
                                expectedItem,
                                index
                            );
                }
            }

            return new AndConstraint<IEnumerable<TValue>>(actualItems);
        }
    }
}
