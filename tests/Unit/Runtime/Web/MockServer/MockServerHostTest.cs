using Anarchy.Toolkit.Runtime.Web.MockServer;
using Anarchy.Toolkit.Runtime.Web.MockServer.Entity;
using Anarchy.Toolkit.Runtime.Web.MockServer.Extensions.Microsoft.AspNetCore.Http;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.MockServer;

public class MockServerHostTest : TestCaseBase, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly string _directory = Directory.CreateTempSubdirectory("mockserver-").FullName;
    private MockServerHost _host = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "orders.json"), """
            [
              { "httpRequest": { "method": "GET", "path": "/orders/{id}" },
                "httpResponse": { "statusCode": 200, "body": { "id": 1 } } },
              { "httpRequest": { "method": "GET", "path": "/orders/42" },
                "httpResponse": { "statusCode": 410, "headers": { "X-Gone": ["yes"] }, "body": "gone" } },
              { "id": "once", "httpRequest": { "path": "/once" }, "httpResponse": { "statusCode": 204 }, "times": { "remainingTimes": 1 } }
            ]
            """);

        var options = new MockServerOptions {
            Port = 0,
            Verbosity = MockServerVerbosity.Quiet,
            RecordFile = Path.Combine(_directory, "requests.jsonl"),
        };
        options.ExpectationPaths.Add(_directory);

        _host = await MockServerHost.StartAsync(options, server => server
            .Use(async (context, next) => {
                if (context.Request.Headers.ContainsKey("X-Fail"))
                {
                    context.Response.StatusCode = 503;
                    context.GetMockServer().AnsweredBy = "fail middleware";
                    return;
                }
                await next(context);
            }, "fail middleware")
            .UseMiddleware<PassThroughMiddleware>()
            .AddExpectation("POST", "/code", 201, new { created = true }));
        _client = new HttpClient { BaseAddress = _host.BaseAddress };
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.DisposeAsync();
        Directory.Delete(_directory, true);
    }

    [Fact]
    public async Task Request_AnswersWithTheMostSpecificExpectation()
    {
        var templated = await _client.GetAsync("/orders/1", Ct);
        var exact = await _client.GetAsync("/orders/42", Ct);

        templated.StatusCode.Should().Be(HttpStatusCode.OK);
        templated.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        (await templated.Content.ReadAsStringAsync(Ct)).Should().Be("""{ "id": 1 }""");
        exact.StatusCode.Should().Be(HttpStatusCode.Gone);
        exact.Headers.GetValues("X-Gone").Should().Equal("yes");
        (await exact.Content.ReadAsStringAsync(Ct)).Should().Be("gone");
    }

    [Fact]
    public async Task Request_HonoursTimes()
    {
        (await _client.GetAsync("/once", Ct)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync("/once", Ct)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_RunsConsumerMiddlewaresAndCodeExpectations()
    {
        var failing = new HttpRequestMessage(HttpMethod.Get, "/orders/1") { Headers = { { "X-Fail", "1" } } };

        (await _client.SendAsync(failing, Ct)).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await _client.PostAsync("/code", null, Ct)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ControlApi_RecordsRequestsAndAcceptsRuntimeExpectations()
    {
        var put = await _client.PutAsync("/__mock/expectations", new StringContent(
            """{ "httpRequest": { "method": "POST", "path": "/runtime", "body": { "name": "x" } }, "httpResponse": { "statusCode": 202 } }""",
            Encoding.UTF8, "application/json"), Ct);
        put.StatusCode.Should().Be(HttpStatusCode.Created);

        var post = await _client.PostAsync("/runtime", new StringContent("""{ "name": "x", "extra": 1 }""", Encoding.UTF8, "application/json"), Ct);
        post.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var recorded = await _client.GetFromJsonAsync<List<RecordedRequest>>("/__mock/requests?method=POST&path=/runtime", Ct);
        recorded.Should().ContainSingle().Which.MatchedExpectation.Should().Be("runtime (POST /runtime)");

        var lines = await File.ReadAllLinesAsync(Path.Combine(_directory, "requests.jsonl"), Ct);
        lines.Select(line => JsonSerializer.Deserialize<JsonElement>(line).GetProperty("path").GetString()).Should().Contain("/runtime");

        await _client.PostAsync("/__mock/reset", null, Ct);
        (await _client.GetFromJsonAsync<List<RecordedRequest>>("/__mock/requests", Ct)).Should().BeEmpty();
        (await _client.PostAsync("/runtime", null, Ct)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ControlApi_ListsMiddlewaresInOrder()
    {
        var middlewares = await _client.GetFromJsonAsync<List<MockServerMiddleware>>("/__mock/middlewares", Ct);

        middlewares.Should().Equal(
            new MockServerMiddleware(1, "fail middleware", "delegate"),
            new MockServerMiddleware(2, nameof(PassThroughMiddleware), "middleware"));
    }

    [Fact]
    public async Task ControlApi_ReturnsLogsWhateverTheVerbosity()
    {
        await _client.GetAsync("/orders/1", Ct);
        await _client.GetAsync("/nothing", Ct);

        var logs = await _client.GetFromJsonAsync<List<MockServerLogEntry>>("/__mock/logs", Ct);

        logs.Should().Contain(e => e.Level == MockServerLogLevel.Request && e.Request!.Path == "/orders/1");
        logs.Should().Contain(e => e.Level == MockServerLogLevel.Warning && e.Message.Contains("/nothing"));
        logs!.Select(e => e.Sequence).Should().BeInAscendingOrder();

        var after = await _client.GetFromJsonAsync<List<MockServerLogEntry>>($"/__mock/logs?after={logs![^1].Sequence}", Ct);
        after.Should().NotContain(e => e.Request != null && e.Request.Path == "/orders/1");
    }

    private sealed class PassThroughMiddleware(RequestDelegate next)
    {
        public Task InvokeAsync(HttpContext context) => next(context);
    }
}
