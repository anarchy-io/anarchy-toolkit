using AwesomeAssertions;
using AwesomeAssertions.Collections;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions
{
    public class InMemorySinkAssertions : ReferenceTypeAssertions<InMemorySink, InMemorySinkAssertions>
    {
        private readonly AssertionChain _assertionChain;

        public InMemorySinkAssertions(InMemorySink subject, AssertionChain assertionChain) : base(subject, assertionChain)
        {
            _assertionChain = assertionChain;
        }

        public AndWhichConstraint<LogEventAssertions, LogEvent> HaveMessage(
            string messageTemplate,
            string because = "",
            params object[] becauseArgs
        )
        {
            var matches = Subject.Messages.Where(m => m.MessageTemplate.Text == messageTemplate).ToList();

            if (matches.Count > 1)
            {
                _assertionChain
                    .BecauseOf(because, becauseArgs)
                    .ForCondition(matches.Count == 1)
                    .FailWith(
                        "Expected {0} to have a single log message with template '{1}', but it was found {2} times",
                        Identifier,
                        messageTemplate,
                        matches.Count
                    );
            }

            var logEvent = matches.SingleOrDefault();

            _assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(logEvent != null)
                .FailWith(
                    "Expected {0} to have log message with template '{1}', but it wasn't found",
                    Identifier,
                    messageTemplate
                );

            return new AndWhichConstraint<LogEventAssertions, LogEvent>(
                new LogEventAssertions(logEvent!, _assertionChain),
                logEvent!
            );
        }

        public AndWhichConstraint<GenericCollectionAssertions<LogEvent>, IEnumerable<LogEvent>> HaveMessages(
            string messageTemplate,
            string because = "",
            params object[] becauseArgs
        )
        {
            var matches = Subject.Messages.Where(m => m.MessageTemplate.Text == messageTemplate)
                                 .ToList();

            _assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(matches.Any())
                .FailWith(
                    "Expected {0} to have at least one log message with template '{1}', but it wasn't found",
                    Identifier,
                    messageTemplate
                );

            return new AndWhichConstraint<GenericCollectionAssertions<LogEvent>, IEnumerable<LogEvent>>(
                new GenericCollectionAssertions<LogEvent>(matches, _assertionChain),
                matches
            );
        }

        public AndWhichConstraint<LogEventAssertions, LogEvent> HaveMatch(
            string wildcardPattern,
            string because = "",
            params object[] becauseArgs
        )
        {
            var matches = AllThatMatch(wildcardPattern).ToList();

            if (matches.Count > 1)
            {
                _assertionChain
                    .BecauseOf(because, becauseArgs)
                    .ForCondition(matches.Count == 1)
                    .FailWith(
                        "Expected {0} to have a single log message which match '{1}', but it was found {2} times",
                        Identifier,
                        wildcardPattern,
                        matches.Count
                    );
            }

            var logEvent = matches.SingleOrDefault();

            _assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(logEvent != null)
                .FailWith(
                    "Expected {0} to have a log message which match '{1}', but it wasn't found",
                    Identifier,
                    wildcardPattern
                );

            return new AndWhichConstraint<LogEventAssertions, LogEvent>(
                new LogEventAssertions(logEvent!, _assertionChain),
                logEvent!
            );
        }

        public AndWhichConstraint<GenericCollectionAssertions<LogEvent>, IEnumerable<LogEvent>> HaveMatches(
            string wildcardPattern,
            string because = "",
            params object[] becauseArgs
        )
        {
            var matches = AllThatMatch(wildcardPattern).ToList();

            _assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(matches.Any())
                .FailWith(
                    "Expected {0} to have at least one log message which match '{1}', but it wasn't found",
                    Identifier,
                    wildcardPattern
                );

            return new AndWhichConstraint<GenericCollectionAssertions<LogEvent>, IEnumerable<LogEvent>>(
                new GenericCollectionAssertions<LogEvent>(matches, _assertionChain),
                matches
            );
        }

        /// <summary>
        /// This is copied from the internal implementation of <see cref="AwesomeAssertions.Collections.StringCollectionAssertions"/>
        /// https://github.com/fluentassertions/fluentassertions/blob/577d555b10d3e393dd3284c1cc3f8ba696def617/Src/FluentAssertions/Collections/StringCollectionAssertions.cs#L274
        /// </summary>
        private IEnumerable<LogEvent> AllThatMatch(string wildcardPattern)
        {
            return Subject.Messages.Where(item => {
                using (var scope = new AssertionScope())
                {
                    item.MessageTemplate.Text.Should().Match(wildcardPattern);
                    return !scope.Discard().Any();
                }
            });
        }

        protected override string Identifier { get; } = "InMemorySink";
    }
}
