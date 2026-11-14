using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Serilog.Events;
using Serilog.Parsing;
using Xunit.Sdk;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Assertions;

public class InMemorySinkAssertionsTests
{
    [Fact]
    public void HaveMessage_GivenMessage_ReturnsIt()
    {
        var message = Guid.NewGuid().ToString();
        var expected = AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMessage(message);

        result.Which.Should().BeSameAs(expected);
    }

    [Fact]
    public void HaveMessage_GivenMultipleMessages_WhenSingleMatch_ReturnsIt()
    {
        var message1 = Guid.NewGuid().ToString();
        var message2 = Guid.NewGuid().ToString();
        var expected = AddMessageToSink(message1);
        AddMessageToSink(message2);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMessage(message1);

        result.Which.Should().BeSameAs(expected);
    }

    [Fact]
    public void HaveMessage_GivenMultipleMessages_WhenMatches_ThrowsXunitException()
    {
        var message = Guid.NewGuid().ToString();
        AddMessageToSink(message);
        AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveMessage(message))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""InMemorySink"" to have a single log message with template '""{message}""', but it was found 2 times"
            );
    }

    [Fact]
    public void HaveMessage_WhenMessageNotFound_ThrowsXunitException()
    {
        var message = Guid.NewGuid().ToString();
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveMessage(message))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""InMemorySink"" to have log message with template '""{message}""', but it wasn't found"
            );
    }

    [Fact]
    public void HaveMessages_GivenMessage_ReturnsIt()
    {
        var message = Guid.NewGuid().ToString();
        var expected = AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMessages(message);

        result.Which.Should().BeEquivalentTo([expected]);
    }

    [Fact]
    public void HaveMessages_GivenMultipleMessages_WhenSingleMatch_ReturnsIt()
    {
        var message1 = Guid.NewGuid().ToString();
        var message2 = Guid.NewGuid().ToString();
        var expected = AddMessageToSink(message1);
        AddMessageToSink(message2);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMessages(message1);

        result.Which.Should().BeEquivalentTo([expected]);
    }

    [Fact]
    public void HaveMessages_GivenMultipleMessages_ReturnsThem()
    {
        var message = Guid.NewGuid().ToString();
        var message1 = AddMessageToSink(message);
        var message2 = AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMessages(message);

        result.Which.Should().BeEquivalentTo([message1, message2]);
    }

    [Fact]
    public void HaveMessages_WhenMessageNotFound_ThrowsXunitException()
    {
        var message = Guid.NewGuid().ToString();
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveMessages(message))
           .Should().Throw<XunitException>()
           .Which.Message.Should().Be(
                $@"Expected ""InMemorySink"" to have at least one log message with template '""{message}""', but it wasn't found"
            );
    }

    [Theory]
    [MemberData(nameof(WildcardsTestData))]
    public void HaveMatch_GivenMatch_ReturnsIt(string wildcard, string message)
    {
        var expected = AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMatch(wildcard);

        result.Which.Should().BeSameAs(expected);
    }

    [Fact]
    public void HaveMatch_GivenMultipleMessages_ThrowsXunitException()
    {
        var message = Guid.NewGuid().ToString();
        var wildcard = "*";
        AddMessageToSink(message);
        AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveMatch(wildcard))
            .Should().Throw<XunitException>()
            .Which.Message.Should().Be(
                $@"Expected ""InMemorySink"" to have a single log message which match '""{wildcard}""', but it was found 2 times"
            );
    }

    [Fact]
    public void HaveMatch_WhenMatchNotFound_ThrowsXunitException()
    {
        var wildcard = "*";
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveMatch(wildcard))
            .Should().Throw<XunitException>()
            .Which.Message.Should().Be(
                $@"Expected ""InMemorySink"" to have a log message which match '""{wildcard}""', but it wasn't found"
            );
    }

    [Theory]
    [MemberData(nameof(WildcardsTestData))]
    public void HaveMatches_GivenMatch_ReturnsIt(string wildcard, string message)
    {
        var expected = AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMatches(wildcard);

        result.Which.Should().BeEquivalentTo([expected]);
    }

    [Theory]
    [MemberData(nameof(WildcardsTestData))]
    public void HaveMatches_GivenMultipleMessages_ReturnsThem(string wildcard, string message)
    {
        var message1 = AddMessageToSink(message);
        var message2 = AddMessageToSink(message);
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        var result = sut.HaveMatches(wildcard);

        result.Which.Should().BeEquivalentTo([message1, message2]);
    }

    [Fact]
    public void HaveMatches_WhenMessageNotFound_ThrowsXunitException()
    {
        var wildcard = "*";
        var sut = new InMemorySinkAssertions(_sink, AssertionChain.GetOrCreate());

        sut.Invoking(s => s.HaveMatches(wildcard))
            .Should().Throw<XunitException>()
            .Which.Message.Should().Be(
                $@"Expected ""InMemorySink"" to have at least one log message which match '""{wildcard}""', but it wasn't found"
            );
    }

    public static TheoryData<string, string> WildcardsTestData =>
        new() {
            {"*", "any message matches"},
            {"* message", "matches beginning of the message"},
            {"message *", "message ending matches"},
            {"* message *", "middle part of the message matches"},
            {"?", "1"},
            {"matches any ? single character", "matches any X single character"},
            {"any last character matches ?", "any last character matches X"}
        };

    private readonly InMemorySink _sink = new();

    private TestLogEvent AddMessageToSink(string expectedMessage)
    {
        var logEvent = new TestLogEvent(messageTemplate: new MessageTemplate(
        [
            new TextToken(expectedMessage)
        ]));
        _sink.Emit(logEvent);
        return logEvent;
    }
}
