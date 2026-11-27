using Anarchy.Toolkit.Runtime.Web.MockServer.Entity;
using Anarchy.Toolkit.Runtime.Web.MockServer.Service;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using Spectre.Console;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.MockServer.Service;

public class MockServerLogTest : TestCaseBase
{
    [Fact]
    public void Write_KeepsTheLastEntriesInOrder()
    {
        var log = new MockServerLog(limit: 2);

        log.Write(MockServerLogLevel.Information, "a");
        log.Write(MockServerLogLevel.Warning, "b");
        log.Write(MockServerLogLevel.Error, "c");

        log.All.Select(e => (e.Sequence, e.Message)).Should().Equal((2, "b"), (3, "c"));
        log.After(2).Should().ContainSingle().Which.Message.Should().Be("c");
    }

    [Fact]
    public void Write_NotifiesFollowers()
    {
        var log = new MockServerLog();
        var seen = new List<string>();
        log.EntryWritten += entry => seen.Add(entry.Message);

        log.Write(MockServerLogLevel.Information, "hello");

        seen.Should().Equal("hello");
    }

    [Fact]
    public void Console_WritesToTheLogEvenWhenQuiet()
    {
        var output = new StringWriter();
        var console = new MockServerConsole(new MockServerOptions { Verbosity = MockServerVerbosity.Quiet },
            AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) }));

        console.Info("info");
        console.Request(new RecordedRequest { Method = "GET", Path = "/x", StatusCode = 200, MatchedExpectation = "x" }, control: false, null);
        console.Request(new RecordedRequest { Method = "GET", Path = "/__mock/health", StatusCode = 200 }, control: true, null);

        output.ToString().Should().BeEmpty();
        console.Log.All.Select(e => e.Level).Should().Equal(MockServerLogLevel.Information, MockServerLogLevel.Request);
    }
}
