using Anarchy.Toolkit.Core.Exceptions;
using AwesomeAssertions;
using AwesomeAssertions.Collections;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Numeric;
using AwesomeAssertions.Primitives;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;

/// <summary>
/// AwesomeAssertions assertions for a Serilog <see cref="LogEvent"/>, focused on its structured properties.
/// </summary>
/// <remarks>Typically reached via <see cref="InMemorySinkAssertions.HaveMessage"/> or <see cref="LogEventExtensions.Should(LogEvent)"/>.</remarks>
public class LogEventAssertions : ReferenceTypeAssertions<LogEvent, LogEventAssertions>
{
    private readonly AssertionChain _assertionChain;

    /// <summary>Initializes a new instance of the <see cref="LogEventAssertions"/> class.</summary>
    /// <param name="subject">The log event under test.</param>
    /// <param name="assertionChain">The assertion chain used to report failures.</param>
    public LogEventAssertions(LogEvent subject, AssertionChain assertionChain) : base(subject, assertionChain)
    {
        _assertionChain = assertionChain;
    }

    /// <inheritdoc/>
    protected override string Identifier { get; } = "LogEvent";

    /// <summary>
    /// Assert that the log event contains a property with a specified name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>A constraint whose <c>Which</c> is the raw <see cref="LogEventPropertyValue"/>.</returns>
    public AndWhichConstraint<ObjectAssertions, LogEventPropertyValue> HaveProperty(
        string name,
        string because = "",
        params object[] becauseArgs
    )
    {
        _assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Properties.ContainsKey(name))
            .FailWith(
                "Expected {0} to have property {1}, but it wasn't found",
                Identifier,
                name
            );

        var logEventProperty = Subject.Properties[name];

        return new AndWhichConstraint<ObjectAssertions, LogEventPropertyValue>(
            new ObjectAssertions(logEventProperty, _assertionChain), logEventProperty);
    }

    /// <summary>
    /// Assert that the log event contains a scalar property with a specified name and return its value as object.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>The value of the property.</returns>
    public AndWhichConstraint<ObjectAssertions, object?> HaveObjectProperty(
        string name,
        string because = "",
        params object[] becauseArgs
    )
    {
        var logEventProperty = HaveProperty(name, because, becauseArgs).Which;

        _assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(logEventProperty is ScalarValue)
            .FailWith(
                "Expected {0} to have property {1} of type {2}, but found {3}",
                Identifier,
                name,
                typeof(ScalarValue),
                logEventProperty
            );

        var value = ((ScalarValue)logEventProperty).Value;

        return new AndWhichConstraint<ObjectAssertions, object?>(new ObjectAssertions(value, _assertionChain), value);
    }

    /// <summary>
    /// Assert that the log event contains a sequence property with a specified name and return its elements as an array.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>The value of the property.</returns>
    /// <remarks>Nested scalar, sequence and dictionary values are unwrapped to plain objects, arrays and dictionaries. Dictionaries are not supported yet, since those are treated by Serilog as SequenceValue on the root object level</remarks>
    public AndWhichConstraint<GenericCollectionAssertions<object?>, object?[]> HaveCollectionProperty(
        string name,
        string because = "",
        params object[] becauseArgs
    )
    {
        var logEventProperty = HaveProperty(name, because, becauseArgs).Which;

        _assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(logEventProperty is SequenceValue)
            .FailWith(
                "Expected {0} to have property {1} of type {2}, but found {3}",
                Identifier,
                name,
                typeof(SequenceValue),
                logEventProperty
            );

        var value = ((SequenceValue)logEventProperty).Elements.Select(GetValue).ToArray();

        return new AndWhichConstraint<GenericCollectionAssertions<object?>, object?[]>(
            new GenericCollectionAssertions<object?>(value, _assertionChain),
            value
        );
    }

    /// <summary>
    /// Assert that the log event contains a property with a specified name and a value of type <see cref="string"/>.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <returns>The value of the property.</returns>
    public AndWhichConstraint<StringAssertions, string> HaveStringProperty(
        string name,
        string because = "",
        params object[] becauseArgs
    )
    {
        var logEventProperty = HaveProperty(name, because, becauseArgs).Which;

        _assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(
                logEventProperty is ScalarValue scalarValue
                && scalarValue.Value is string
            )
            .FailWith(
                "Expected {0} to have property {1} of type {2} with a string value, but found {3}",
                Identifier,
                name,
                typeof(ScalarValue),
                logEventProperty
            );

        var value = (string)((ScalarValue)logEventProperty).Value!;

        return new AndWhichConstraint<StringAssertions, string>(new StringAssertions(value, _assertionChain), value);
    }

    /// <summary>
    /// Assert that the log event contains a property with a specified name and a <see cref="ScalarValue"/> whose value is of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <returns>The value of the property.</returns>
    /// <remarks>Alias of <see cref="HaveNumericProperty{T}"/>.</remarks>
    public AndWhichConstraint<NumericAssertions<T>, T> HaveScalarProperty<T>(
        string name,
        string because = "",
        params object[] becauseArgs
    ) where T : struct, IComparable<T> =>
        HaveNumericProperty<T>(name, because, becauseArgs);

    /// <summary>
    /// Assert that the log event contains a property with a specified name and a <see cref="ScalarValue"/> whose value is of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="because">A formatted phrase explaining why the condition should be satisfied. If the phrase does not start with the word because, it is prepended to the message.</param>
    /// <param name="becauseArgs">Zero or more values to use for filling in any <see cref="string.Format(string,object[])"/> compatible placeholders.</param>
    /// <typeparam name="T">The expected value type; the scalar value must be exactly of this type (no conversion).</typeparam>
    /// <returns>The value of the property.</returns>
    public AndWhichConstraint<NumericAssertions<T>, T> HaveNumericProperty<T>(
        string name,
        string because = "",
        params object[] becauseArgs
    )
        where T : struct, IComparable<T>
    {
        var logEventProperty = HaveProperty(name, because, becauseArgs).Which;

        _assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(
                logEventProperty is ScalarValue scalarValue
                && scalarValue.Value is T
            )
            .FailWith(
                "Expected {0} to have property {1} of type " + typeof(ScalarValue).FullName + " with a " + typeof(T).FullName + " value, but found {2}",
                Identifier,
                name,
                logEventProperty
            );

        var value = (T)((ScalarValue)logEventProperty).Value!;

        return new AndWhichConstraint<NumericAssertions<T>, T>(new NumericAssertions<T>(value, _assertionChain), value);
    }

    private static object? GetValue(LogEventPropertyValue log) =>
        log switch {
            ScalarValue scalarValue => scalarValue.Value,
            SequenceValue sequenceValue => sequenceValue.Elements.Select(GetValue).ToArray(),
            DictionaryValue dictionaryValue => dictionaryValue.Elements.ToDictionary(
                keySelector: x => x.Key.Value!,
                elementSelector: x => GetValue(x.Value)
            ),
            _ => throw new UnsupportedTypeException(log.GetType())
        };
}
