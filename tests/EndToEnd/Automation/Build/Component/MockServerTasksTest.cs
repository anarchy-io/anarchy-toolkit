using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using AwesomeAssertions;
using Fallout.Common.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.EndToEnd.Automation.Build.Component;

public class MockServerTasksTest
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    [Fact]
    public async Task StartAndStop_RunTheMockServerInTheBackground()
    {
        var root = AbsolutePath.Create(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..")));
        var project = root / "src" / "Web.MockServer" / "Web.MockServer.csproj";
        var temp = AbsolutePath.Create(Directory.CreateTempSubdirectory("mockserver-tasks-")
            .FullName);

        File.WriteAllText(temp / "ping.json",
            """{ "httpRequest": { "path": "/ping" }, "httpResponse": { "body": "pong" } }""");

        try
        {
            var state = MockServerTasks.Start(project, Configuration,
                MockServerTasks.Arguments(0, [temp / "ping.json"], null, "minimal", null),
                temp, temp / "state.json", temp / "server.log", TimeSpan.FromSeconds(60));

            MockServerTasks.Read(temp / "state.json")
                .Should()
                .Be(state);

            using var http = new HttpClient {
                BaseAddress = new Uri(state.Url)
            };

            (await http.GetStringAsync("/ping", Ct)).Should()
                .Be("pong");

            (await http.GetFromJsonAsync<JsonElement>("/__mock/requests", Ct)).GetArrayLength()
                .Should()
                .Be(1);

            var starting = () => MockServerTasks.Start(project, Configuration, [], temp, temp / "state.json",
                temp / "server.log", TimeSpan.FromSeconds(5));

            starting.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*already running*");

            MockServerTasks.Stop(temp / "state.json")
                .Should()
                .Be(state);

            MockServerTasks.Read(temp / "state.json")
                .Should()
                .BeNull();

            var afterStop = async () => await http.GetStringAsync("/ping", Ct);
            await afterStop.Should()
                .ThrowAsync<HttpRequestException>();
        }
        finally
        {
            MockServerTasks.Stop(temp / "state.json");
            Directory.Delete(temp, true);
        }
    }
}
