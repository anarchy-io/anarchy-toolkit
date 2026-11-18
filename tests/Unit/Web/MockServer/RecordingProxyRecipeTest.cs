using Anarchy.Toolkit.Web.MockServer;
using Anarchy.Toolkit.Web.MockServer.Entity;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Web.MockServer;

/// <summary>
/// Keeps the forwarding and recording recipe of docs/guides/mock-server.md compiling and working.
/// </summary>
public class RecordingProxyRecipeTest
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Middleware_ForwardsUnmatchedRequestsAndSavesThemAsExpectations()
    {
        var output = Path.Combine(Path.GetTempPath(), "anarchy-recording-" + Guid.NewGuid());
        var quiet = new MockServerOptions { Port = 0, Verbosity = MockServerVerbosity.Quiet };
        try
        {
            await using var upstream = await MockServerHost.StartAsync(quiet, server => server
                .AddExpectation("GET", "/carriers/dhl", 200, new { name = "DHL" }), Ct);
            await using var proxy = await MockServerHost.StartAsync(
                new MockServerOptions { Port = 0, Verbosity = MockServerVerbosity.Quiet },
                server => server
                    .AddExpectation("GET", "/known", 204)
                    .Use(RecordingProxy.Create(upstream.BaseAddress, output), name: "recording proxy"), Ct);
            using var client = new HttpClient { BaseAddress = proxy.BaseAddress };

            (await client.GetStringAsync("/carriers/dhl", Ct)).Should().Contain("DHL");
            (await client.GetAsync("/known", Ct)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            var saved = Directory.GetFiles(output).Should().ContainSingle().Subject;
            await using var replay = await MockServerHost.StartAsync(quiet, server => server.AddExpectationsFrom(saved), Ct);
            using var replayClient = new HttpClient { BaseAddress = replay.BaseAddress };
            (await replayClient.GetStringAsync("/carriers/dhl", Ct)).Should().Contain("DHL");
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, true);
        }
    }

    // Same code as the guide's recipe.
    private static class RecordingProxy
    {
        public static Func<HttpContext, RequestDelegate, Task> Create(Uri upstream, string directory)
        {
            var http = new HttpClient { BaseAddress = upstream };
            Directory.CreateDirectory(directory);
            return async (context, next) => {
                var mock = context.GetMockServer();
                if (mock.Expectation is not null)
                {
                    await next(context);
                    return;
                }

                var request = mock.Request;
                using var forward = new HttpRequestMessage(new HttpMethod(request.Method), request.Path + context.Request.QueryString);
                if (request.Body is { Length: > 0 } body)
                    forward.Content = new StringContent(body, System.Text.Encoding.UTF8, context.Request.ContentType ?? "application/json");
                using var response = await http.SendAsync(forward, context.RequestAborted);
                var content = await response.Content.ReadAsStringAsync(context.RequestAborted);

                var expectation = new JsonObject {
                    ["httpRequest"] = new JsonObject { ["method"] = request.Method, ["path"] = request.Path },
                    ["httpResponse"] = new JsonObject {
                        ["statusCode"] = (int)response.StatusCode,
                        ["headers"] = new JsonObject { ["Content-Type"] = new JsonArray(response.Content.Headers.ContentType?.ToString() ?? "application/json") },
                        ["body"] = content
                    }
                };
                var name = $"{request.Method}{request.Path.Replace('/', '_')}.json".ToLowerInvariant();
                await File.WriteAllTextAsync(Path.Combine(directory, name),
                    expectation.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), context.RequestAborted);

                context.Response.StatusCode = (int)response.StatusCode;
                context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
                await context.Response.WriteAsync(content, context.RequestAborted);
                mock.AnsweredBy = "recording proxy";
            };
        }
    }
}
