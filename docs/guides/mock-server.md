# Mock Server

How to stand in for the HTTP services an application calls, in integration and end-to-end tests and while developing:
`Anarchy.Toolkit.Web.MockServer` (the server), `Anarchy.Toolkit.Testing.MockServer` (assertions) and the
`IMockServerBuild` targets (lifecycle). The package READMEs list every option; this guide is about choosing and
combining them.

## What to use when

| Need | Use |
|---|---|
| Fixed answers for an upstream API | Expectation files (`tests/mocks/**/*.json`), loaded with `--expectations` / `MockServerExpectations`. |
| Answers that depend on the request (computed bodies, fake auth) | A middleware in your own host (`server.Use(...)`), see [Your own host](#your-own-host). |
| A test that needs one specific answer | A runtime expectation: `MockServerClient.AddExpectationAsync`, reset with `ResetAsync`. |
| Asserting what the application sent | `AssertReceivedAsync` / `ShouldHaveReceived` while the server runs, or `RecordedRequestsFile.Read` afterwards. |
| Failing on calls nothing mocks | `ShouldAllHaveBeenMatched()` on the recorded requests. |
| Starting it around the tests | `make test_integration_mocked` / `make test_end_to_end_mocked` (exposes `MOCKSERVER_URL`). |
| Keeping mocks in step with an OpenAPI document | `make mock_server_sync_openapi SPEC=<file-or-url>`. |
| Recording a real upstream's answers as expectations | A forwarding middleware, see [Recording a real upstream](#recording-a-real-upstream). |
| Editing expectations while the server runs | `--watch` (or `ARGS=--watch`), or `POST /__mock/reload`. |
| Looking at what happened | The UI at `/__mock/ui` (with `Anarchy.Toolkit.Web.MockServer.Ui`), `-v detailed`, or `GET /__mock/logs`. |

## Setting it up

1. Add a runnable project whose name ends in `MockServer` (for example `src/Api/MockServer`), referencing
   `Anarchy.Toolkit.Web.MockServer`, with `return await MockServerEntryPoint.RunAsync(args);` as its `Program.cs`.
2. Implement `IMockServerBuild` in `build/Build.cs` and point it at the expectations, in
   `.fallout/parameters.json` (`"MockServerExpectations": ["tests/mocks"]`) or in code.
3. Point the application's upstream base URLs at the mock server in its test settings. The targets set
   `MOCKSERVER_URL`; tests that run on their own (from an IDE) use a `MockServerFixture`, which spawns the server
   when that variable is missing.
4. Run `make test_end_to_end_mocked`: it starts the server, runs the tests, records every request to
   `artifacts/mock-server/requests.jsonl` and stops it.

## Expectations

Expectations use [MockServer](https://www.mock-server.com/)'s JSON shape: one object or an array per file, with `httpRequest` (method, path, query, headers, body matchers)
and `httpResponse` (status, headers, body, delay), plus `priority` and `times`. Paths can be exact, `{id}` templates
or regular expressions. Keep one file per upstream endpoint or scenario, under a folder per upstream.

## Asserting

```csharp
public class QuoteTests(CarriersMockServer mock) : IClassFixture<CarriersMockServer>
{
    [Fact]
    public async Task Quote_AsksEveryCarrier()
    {
        await mock.Client.ResetAsync(TestContext.Current.CancellationToken);
        // … call the application …
        await mock.Client.AssertReceivedAsync("POST", "/carriers/{carrier}/quotes", times: 2);
    }
}
```

To check a whole run instead, after the server stopped, read the record file and fail on anything no expectation
answered:

```csharp
RecordedRequestsFile.Read("artifacts/mock-server/requests.jsonl").ShouldAllHaveBeenMatched();
```

## Your own host

When fixed answers are not enough, write the host's `Program.cs` yourself and plug in middlewares. They run after
expectation matching: `context.GetMockServer()` gives the parsed request and the matched `Expectation` (or
`null`), and a middleware either answers (setting `AnsweredBy`, which the records and the UI show) or calls `next`.

```csharp
return await MockServerEntryPoint.RunAsync(args, server => server
    .AddExpectationsFrom("tests/mocks")
    .Use(async (context, next) => {
        var mock = context.GetMockServer();
        if (mock.Expectation is null && mock.Request.Path.StartsWith("/stock", StringComparison.Ordinal))
        {
            await context.Response.WriteAsJsonAsync(new { available = 3 });
            mock.AnsweredBy = "stock middleware";
            return;
        }
        await next(context);
    }, name: "stock middleware"));
```

## Recording a real upstream

The server has no built-in proxy mode: recording is rare (refreshing mocks after an upstream changed), needs
credentials and decisions about what to keep, and fits in one middleware. Forward what no expectation matches to the
real service and save each answer as an expectation file, then review the files, strip anything sensitive and commit
them:

```csharp
var upstream = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("RECORD_UPSTREAM")!) };
const string output = "tests/mocks/recorded";
Directory.CreateDirectory(output);

return await MockServerEntryPoint.RunAsync(args, server => server
    .AddExpectationsFrom("tests/mocks")
    .Use(async (context, next) => {
        var mock = context.GetMockServer();
        if (mock.Expectation is not null || upstream.BaseAddress is null)
        {
            await next(context);
            return;
        }

        var request = mock.Request;
        using var forward = new HttpRequestMessage(new HttpMethod(request.Method), request.Path + context.Request.QueryString);
        if (request.Body is { Length: > 0 } body)
            forward.Content = new StringContent(body, Encoding.UTF8, context.Request.ContentType ?? "application/json");
        using var response = await upstream.SendAsync(forward, context.RequestAborted);
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
        await File.WriteAllTextAsync(Path.Combine(output, name),
            expectation.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
        await context.Response.WriteAsync(content, context.RequestAborted);
        mock.AnsweredBy = "recording proxy";
    }, name: "recording proxy"));
```

Run it with `RECORD_UPSTREAM=https://… make mock_server`, exercise the application, and stop it. Add query, header
or body matchers to the saved `httpRequest` when one path has several answers. The toolkit's unit tests run this
recipe, so it stays correct.

## What it covers

Matching on JSON bodies, regular expressions, `times` and `priority` follow [MockServer](https://www.mock-server.com/)'s expectation format. Callbacks, templates (Velocity, JavaScript) and proxy forwarding are not built in: write a middleware, such as the [recording middleware](#recording-a-real-upstream), and assert on what was received with `AssertReceivedAsync` / `ShouldHaveReceived`, or on unmocked calls with `ShouldAllHaveBeenMatched`. `IMockServerBuild` starts the server around the tests, so no container is needed.
