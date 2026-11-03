using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions
{
    /// <inheritdoc />
    public class InMemoryMeasurerAssertions : ReferenceTypeAssertions<InMemoryMeasurer, InMemoryMeasurerAssertions>
    {
        private readonly AssertionChain _assertionChain;

        /// <inheritdoc />
        public InMemoryMeasurerAssertions(InMemoryMeasurer subject, AssertionChain assertionChain) : base(subject, assertionChain)
        {
            _assertionChain = assertionChain;
        }

        /// <inheritdoc />
        protected override string Identifier { get; } = "metrics";

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a counter with the given name
        /// </summary>
        /// <param name="name">The name of the counter</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<double>> NotHaveCounter(string name, string because = "", params object[] becauseArgs) =>
            NotHaveMetricOfType<double>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeCounter);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a counter with the given name
        /// </summary>
        /// <param name="name">The name of the counter</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<double>> HaveCounter(string name, string because = "", params object[] becauseArgs) =>
            HaveMetricOfType<double>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeCounter);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a gauge with the given name
        /// </summary>
        /// <param name="name">The name of the gauge</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<double>> HaveGauge(string name, string because = "", params object[] becauseArgs) =>
            HaveMetricOfType<double>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeGauge);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a gauge with the given name
        /// </summary>
        /// <param name="name">The name of the gauge</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<double>> NotHaveGauge(string name, string because = "", params object[] becauseArgs) =>
            NotHaveMetricOfType<double>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeGauge);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has an event with the given name
        /// </summary>
        /// <param name="name">The name of the counter</param>
        /// <param name="because">A formatted event explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<string>> HaveEvent(string name, string because = "", params object[] becauseArgs) =>
            HaveMetricOfType<string>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeEvent);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has an event with the given name
        /// </summary>
        /// <param name="name">The name of the event</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<string>> NotHaveEvent(string name, string because = "", params object[] becauseArgs) =>
            NotHaveMetricOfType<string>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeEvent);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a timer with the given name
        /// </summary>
        /// <param name="name">The name of the timer</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<TimeSpan>> HaveTimer(string name, string because = "", params object[] becauseArgs) =>
            HaveMetricOfType<TimeSpan>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeTimer);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a timer with the given name
        /// </summary>
        /// <param name="name">The name of the timer</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<TimeSpan>> NotHaveTimer(string name, string because = "", params object[] becauseArgs) =>
            NotHaveMetricOfType<TimeSpan>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeTimer);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a histogram with the given name
        /// </summary>
        /// <param name="name">The name of the histogram</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<double>> HaveHistogram(string name, string because = "", params object[] becauseArgs) =>
            HaveMetricOfType<double>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeHistogram);

        /// <summary>
        /// Asserts that the <see cref="InMemoryMeasurer"/> has a histogram with the given name
        /// </summary>
        /// <param name="name">The name of the histogram</param>
        /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
        /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
        /// <returns></returns>
        public AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<double>> NotHaveHistogram(string name, string because = "", params object[] becauseArgs) =>
            NotHaveMetricOfType<double>(name, because, becauseArgs, InMemoryMeasurer.MetricTypeHistogram);

        private AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<TValue>> HaveMetricOfType<TValue>(
            string name,
            string because,
            object[] becauseArgs,
            string type
        ) =>
            MetricAssertion<TValue>(
                name: name,
                because: because,
                becauseArgs: becauseArgs,
                type: type,
                condition: metric => metric != null,
                message: $"Expected {{context:metrics}} to contain a {type} '{name}', but it does not exist."
            );

        private AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<TValue>> NotHaveMetricOfType<TValue>(
            string name,
            string because,
            object[] becauseArgs,
            string type
        ) =>
            MetricAssertion<TValue>(
                name: name,
                because: because,
                becauseArgs: becauseArgs,
                type: type,
                condition: metric => metric == null,
                message: $"Expected {{context:metrics}} to not contain a {type} '{name}', but it exists."
            );

        private AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<TValue>> MetricAssertion<TValue>(
            string name,
            string because,
            object[] becauseArgs,
            string type,
            Predicate<Metric?> condition,
            string message
        )
        {
            var metric = Subject.Metrics.SingleOrDefault(m => m.Name == name && m.Type == type);

            _assertionChain
                   .BecauseOf(because, becauseArgs)
                   .ForCondition(condition(metric))
                   .FailWith(message);

            return new AndWhichConstraint<InMemoryMeasurerAssertions, MetricAssertions<TValue>>(
                this,
                new MetricAssertions<TValue>(metric!, name, _assertionChain)
            );
        }
    }
}
