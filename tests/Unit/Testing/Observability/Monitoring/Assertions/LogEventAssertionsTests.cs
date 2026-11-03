using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Serilog.Events;
using Xunit.Sdk;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Assertions;

public class LogEventAssertionsTests
{
    [Fact]
    public void HaveProperty_ReturnsPropertyValue()
    {
        var propertyName = TestData.AnyString();
        var propertyValue = new ScalarValue(new object());
        var logEvent = new TestLogEvent(properties: new LogEventProperty(propertyName, propertyValue));
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        var result = sut.HaveProperty(propertyName);

        result.Which.Should().BeSameAs(propertyValue);
    }

    [Fact]
    public void HaveProperty_GivenNoProperty_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent();
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveProperty(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"", but it wasn't found"
            );
    }

    [Fact]
    public void HaveStringProperty_ReturnsPropertyValue()
    {
        var propertyName = TestData.AnyString();
        var propertyValue = TestData.AnyString();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new ScalarValue(propertyValue))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        var result = sut.HaveStringProperty(propertyName);

        result.Which.Should().Be(propertyValue);
    }

    [Fact]
    public void HaveStringProperty_GivenNonString_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new ScalarValue(new object()))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveStringProperty(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"" of type {typeof(ScalarValue).FullName} with a string value, but found {typeof(object).FullName}"
            );
    }

    [Fact]
    public void HaveStringProperty_GivenNoProperty_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent();
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveStringProperty(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"", but it wasn't found"
            );
    }

    [Fact]
    public void HaveNumericProperty_ReturnsPropertyValue()
    {
        var propertyName = TestData.AnyString();
        var propertyValue = TestData.AnyInt();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new ScalarValue(propertyValue))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        var result = sut.HaveNumericProperty<int>(propertyName);

        result.Which.Should().Be(propertyValue);
    }

    [Fact]
    public void HaveNumericProperty_GivenNonNumeric_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new ScalarValue(new object()))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveNumericProperty<int>(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"" of type {typeof(ScalarValue).FullName} with a {typeof(int).FullName} value, but found {typeof(object).FullName}"
            );
    }

    [Fact]
    public void HaveNumericProperty_GivenNoProperty_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent();
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveNumericProperty<int>(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"", but it wasn't found"
            );
    }

    [Fact]
    public void HaveScalarProperty_ReturnsPropertyValue()
    {
        var propertyName = TestData.AnyString();
        var propertyValue = TestData.AnyChar();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new ScalarValue(propertyValue))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        var result = sut.HaveScalarProperty<char>(propertyName);

        result.Which.Should().Be(propertyValue);
    }

    [Fact]
    public void HaveScalarProperty_GivenNonScalar_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new ScalarValue(new object()))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveScalarProperty<char>(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"" of type {typeof(ScalarValue).FullName} with a {typeof(char).FullName} value, but found {typeof(object).FullName}"
            );
    }

    [Fact]
    public void HaveScalarProperty_GivenNoProperty_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent();
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveScalarProperty<char>(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"", but it wasn't found"
            );
    }

    [Fact]
    public void HaveObjectProperty_ReturnsPropertyValue()
    {
        var propertyName = TestData.AnyString();
        var propertyValue = new object();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new ScalarValue(propertyValue))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        var result = sut.HaveObjectProperty(propertyName);

        result.Which.Should().BeSameAs(propertyValue);
    }

    [Fact]
    public void HaveObjectProperty_GivenNonScalar_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(
                propertyName,
                new StructureValue([]))
            );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveObjectProperty(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"" of type {typeof(ScalarValue).FullName}, but found {{  }}"
            );
    }

    [Fact]
    public void HaveObjectProperty_GivenNoProperty_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent();
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveObjectProperty(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"", but it wasn't found"
            );
    }

    [Fact]
    public void HaveCollectionProperty_ReturnsPropertyValue()
    {
        var propertyName = TestData.AnyString();
        var propertyValue = new LogEventPropertyValue[]
                            {
                                new ScalarValue("1"),
                                new SequenceValue(
                                    [
                                        new ScalarValue(2),
                                        new ScalarValue(DateTime.MinValue)
                                    ]
                                )
                            };
        var expected = new object[] { "1", new object[] { 2, DateTime.MinValue } };
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new SequenceValue(propertyValue))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        var result = sut.HaveCollectionProperty(propertyName);

        result.Which.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void HaveCollectionProperty_GivenEmpty_ReturnsPropertyValue()
    {
        var propertyName = TestData.AnyString();
        var propertyValue = Array.Empty<LogEventPropertyValue>();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(propertyName, new SequenceValue(propertyValue))
        );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        var result = sut.HaveCollectionProperty(propertyName);

        result.Which.Should().BeEmpty();
    }

    [Fact]
    public void HaveCollectionProperty_GivenNonSequence_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent(
            properties: new LogEventProperty(
                propertyName,
                new StructureValue([]))
            );
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveCollectionProperty(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"" of type {typeof(SequenceValue).FullName}, but found {{  }}"
            );
    }

    [Fact]
    public void HaveCollectionProperty_GivenNoProperty_ThrowsXunitException()
    {
        var propertyName = TestData.AnyString();
        var logEvent = new TestLogEvent();
        var sut = new LogEventAssertions(logEvent, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveCollectionProperty(propertyName))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""LogEvent"" to have property ""{propertyName}"", but it wasn't found"
            );
    }
}
