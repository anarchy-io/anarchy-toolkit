using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Component.Tasks;

public class MockServerTasksTest
{
    [Fact]
    public void Arguments_IncludesOnlyGivenOptions()
    {
        MockServerTasks.Arguments(7000, ["a.json", "mocks"], "out.jsonl", "detailed", "--watch  --unmatched-status 500")
            .Should()
            .Equal("--port", "7000", "--expectations", "a.json", "--expectations", "mocks", "--record", "out.jsonl",
                "--verbosity", "detailed", "--watch", "--unmatched-status", "500");

        MockServerTasks.Arguments(null, [], null, null, null)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void Arguments_WithoutUi_PassesNoUi()
    {
        MockServerTasks.Arguments(null, [], null, null, null, ui: false)
            .Should()
            .Equal("--no-ui");
    }

    [Theory]
    [InlineData("MOCKSERVER_READY port=6001 url=http://localhost:6001", 6001, "http://localhost:6001")]
    [InlineData("something else", null, null)]
    public void ParseReady_ReadsPortAndUrl(string line, int? port, string? url) =>
        MockServerTasks.ParseReady(line)
            .Should()
            .Be(port is null ? null : (port.Value, url!));
}
