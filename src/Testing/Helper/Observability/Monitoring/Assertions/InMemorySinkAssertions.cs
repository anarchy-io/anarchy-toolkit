using AwesomeAssertions;
using AwesomeAssertions.Collections;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;

/// <summary>
/// AwesomeAssertions assertions for an <see cref="InMemorySink"/>, finding captured log events by message template.
/// </summary>
/// <remarks>
/// Matching is done against the raw message template text (e.g. <c>"Order {OrderId} created"</c>), not the rendered message.
/// Obtain via <see cref="InMemorySinkExtensions.Should(InMemorySink)"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// observer.SinkLogs.Should()
///     .HaveMessage("Order {OrderId} created")
///     .And.HaveNumericProperty&lt;int&gt;("OrderId").Which.Should().Be(42);
/// </code>
/// </example>
public class InMemorySinkAssertions : ReferenceTypeAssertions<InMemorySink, InMemorySinkAssertions>
{
    private readonly AssertionChain _assertionChain;

    /// <summary>Initializes a new instance of the <see cref="InMemorySinkAssertions"/> class.</summary>
    /// <param name="subject">The sink under test.</param>
    /// <param name="assertionChain">The assertion chain used to report failures.</param>
    public InMemorySinkAssertions(InMemorySink subject, AssertionChain assertionChain) : base(subject, assertionChain)
    {
        _assertionChain = assertionChain;
    }

    /// <summary>
    /// Asserts that exactly one captured log event has the given message template.
    /// </summary>
    /// <param name="messageTemplate">The exact message template text.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>A constraint exposing <see cref="LogEventAssertions"/> via <c>And</c> and the matched <see cref="LogEvent"/> via <c>Which</c>.</returns>
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

    /// <summary>
    /// Asserts that at least one captured log event has the given message template.
    /// </summary>
    /// <param name="messageTemplate">The exact message template text.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>A constraint exposing collection assertions and the matched events.</returns>
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

    /// <summary>
    /// Asserts that exactly one captured log event has a message template matching the wildcard pattern.
    /// </summary>
    /// <param name="wildcardPattern">A pattern where <c>*</c> matches any characters and <c>?</c> a single character.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>A constraint exposing <see cref="LogEventAssertions"/> via <c>And</c> and the matched <see cref="LogEvent"/> via <c>Which</c>.</returns>
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

    /// <summary>
    /// Asserts that at least one captured log event has a message template matching the wildcard pattern.
    /// </summary>
    /// <param name="wildcardPattern">A pattern where <c>*</c> matches any characters and <c>?</c> a single character.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>A constraint exposing collection assertions and the matched events.</returns>
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

    /// <inheritdoc/>
    protected override string Identifier { get; } = "InMemorySink";
}
