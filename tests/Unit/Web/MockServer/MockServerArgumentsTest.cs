using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.MockServer;

public class MockServerArgumentsTest : TestCaseBase
{
    private static MockServerArguments.ParseResult Parse(string[] args, Dictionary<string, string>? environment = null) =>
        MockServerArguments.Parse(args, name => environment?.GetValueOrDefault(name));

    [Fact]
    public void Parse_WithoutArguments_UsesDefaults()
    {
        var result = Parse([]);

        result.Errors.Should().BeEmpty();
        result.Options.Port.Should().BeNull();
        result.Options.Verbosity.Should().Be(MockServerVerbosity.Normal);
    }

    [Fact]
    public void Parse_ReadsEveryOption()
    {
        var result = Parse([
            "--port=7000", "-e", "a.json", "--expectations", "mocks/**/*.json", "-r", "out.jsonl", "--record-limit", "5",
            "--kill-timeout", "30", "--unmatched-status", "500", "-v", "diag", "--watch", "--no-color",
        ]);

        result.Errors.Should().BeEmpty();
        var options = result.Options;
        options.Port.Should().Be(7000);
        options.ExpectationPaths.Should().Equal("a.json", "mocks/**/*.json");
        options.RecordFile.Should().Be("out.jsonl");
        options.RecordLimit.Should().Be(5);
        options.KillTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.UnmatchedStatusCode.Should().Be(500);
        options.Verbosity.Should().Be(MockServerVerbosity.Diagnostic);
        options.WatchExpectations.Should().BeTrue();
        options.NoColor.Should().BeTrue();
    }

    [Fact]
    public void Parse_ReadsUiAndLogBuffer()
    {
        Parse([]).Options.Ui.Should().BeTrue();

        var result = Parse(["--no-ui", "--log-buffer", "50"]);

        result.Errors.Should().BeEmpty();
        result.Options.Ui.Should().BeFalse();
        result.Options.LogBuffer.Should().Be(50);
        Parse([], new() { ["MOCKSERVER_UI"] = "false", ["MOCKSERVER_LOG_BUFFER"] = "7" }).Options
            .Should().BeEquivalentTo(new { Ui = false, LogBuffer = 7 });
    }

    [Fact]
    public void Parse_ArgumentsOverrideEnvironment()
    {
        var result = Parse(["-p", "7001", "-q"], new Dictionary<string, string> {
            ["MOCKSERVER_PORT"] = "7000",
            ["MOCKSERVER_VERBOSITY"] = "detailed",
            ["MOCKSERVER_EXPECTATIONS"] = "a;b",
        });

        result.Options.Port.Should().Be(7001);
        result.Options.Verbosity.Should().Be(MockServerVerbosity.Quiet);
        result.Options.ExpectationPaths.Should().Equal("a", "b");
    }

    [Theory]
    [InlineData("--port", "abc")]
    [InlineData("--verbosity", "loud")]
    [InlineData("--what", "ever")]
    public void Parse_ReportsInvalidArguments(string name, string value) =>
        Parse([name, value]).Errors.Should().NotBeEmpty();
}
