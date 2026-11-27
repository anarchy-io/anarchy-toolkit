# Anarchy.Toolkit.Runtime.Web.MockServer

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

An HTTP mock server for integration and end-to-end tests: JSON expectations, pluggable middlewares, request recording
and colourful console output.

```bash
dotnet add package Anarchy.Toolkit.Runtime.Web.MockServer
```

- Expectations follow [MockServer](https://www.mock-server.com/)'s JSON shape, so existing `expectations/*.json` files work as they are.
- Your own middlewares run between expectation matching and the answer: they can answer by themselves or change the match.
- Every request can be appended to a JSON-lines file, and is queryable through the `/__mock` control API (see `Anarchy.Toolkit.Testing.MockServer` for test assertions).
- It listens on port `6001`, or the next free one when that is taken.

## When to use it

Use it to stand in for the HTTP services an application calls while integration or end-to-end tests run against it.
It is a console application whose lifetime is usually driven by the Anarchy `IMockServerBuild` targets or a Makefile,
but it can also be run by hand, spawned from a test, or started in-process with `MockServerHost.StartAsync`.

## How to use it

Write expectations, one object or an array per file:

```json
[
  {
    "httpRequest": { "method": "GET", "path": "/orders/{id}", "headers": { "Authorization": ["Bearer .+"] } },
    "httpResponse": { "statusCode": 200, "headers": { "Content-Type": ["application/json"] }, "body": { "id": 1 } }
  },
  {
    "httpRequest": { "method": "POST", "path": "/orders", "body": { "type": "JSON", "json": { "customerId": 7 } } },
    "httpResponse": { "statusCode": 201, "delay": { "timeUnit": "MILLISECONDS", "value": 50 } },
    "times": { "remainingTimes": 1 },
    "priority": 10
  }
]
```

- **Paths** are exact (`/orders`), templated (`/orders/{id}`) or regular expressions (`/orders/[0-9]+`); exact beats templated beats regular expression, after `priority`.
- **Query parameters and headers** must contain every listed value (each one a regular expression).
- **Bodies** match when the request contains the given JSON (or equals it with `"matchType": "STRICT"`), a string, `{ "type": "STRING", "string": …, "subString": true }` or `{ "type": "REGEX", "regex": … }`.
- **Responses** write a string body as-is and anything else as JSON.

Run it:

```bash
dotnet Anarchy.Toolkit.Runtime.Web.MockServer.dll -e "mocks/**/*.json" --record artifacts/mock-requests.jsonl -v detailed
```

| Option | Environment | Description |
| --- | --- | --- |
| `-p, --port` | `MOCKSERVER_PORT` | Port (default `6001` or the next free one; `0` lets the OS choose; an explicit port that is taken fails). |
| `--host` | `MOCKSERVER_HOST` | Host (default `localhost`). |
| `-e, --expectations` | `MOCKSERVER_EXPECTATIONS` (`;`-separated) | File, directory or glob; repeatable. |
| `-w, --watch` | `MOCKSERVER_WATCH` | Reload expectations when the files change. |
| `-r, --record` | `MOCKSERVER_RECORD` | Append every request to a JSON-lines file. |
| `--record-limit` | `MOCKSERVER_RECORD_LIMIT` | Requests kept in memory (default 1000, `0` = all). |
| `--log-buffer` | `MOCKSERVER_LOG_BUFFER` | Log entries kept in memory for the UI and `/__mock/logs` (default 1000, `0` = all). |
| `--no-ui` | `MOCKSERVER_UI=false` | Do not serve the UI, on hosts that plug one in (`Anarchy.Toolkit.Runtime.Web.MockServer.Ui`). |
| `--kill-timeout` | `MOCKSERVER_KILL_TIMEOUT` | Stop after this many seconds. |
| `--unmatched-status` | `MOCKSERVER_UNMATCHED_STATUS` | Status when nothing matches (default 404). |
| `-v, --verbosity` | `MOCKSERVER_VERBOSITY` | `quiet`, `minimal`, `normal` (default), `detailed` (headers and bodies) or `diagnostic` (responses and ASP.NET Core logs). |
| `-q, --quiet` | | Same as `--verbosity quiet`. |
| `--no-color` | `NO_COLOR` | Plain output. |

Once listening it always prints `MOCKSERVER_READY port=<port> url=<url>`, whatever the verbosity, for whoever spawned it.

### Your own host and middlewares

```csharp
return await MockServerEntryPoint.RunAsync(args, server => server
    .AddExpectationsFrom("mocks")
    .UseMiddleware<FakeAuthMiddleware>()
    .Use(async (context, next) => {
        var mock = context.GetMockServer();
        if (mock.Request.Path.StartsWith("/stock") && mock.Request.Query.ContainsKey("sku"))
        {
            await context.Response.WriteAsJsonAsync(new { sku = mock.Request.Query["sku"][0], available = 3 });
            mock.AnsweredBy = "stock middleware";
            return;
        }
        await next(context);
    }, name: "stock middleware"));
```

Middlewares are listed, in order, by `GET /__mock/middlewares` and the UI: classes by type name, delegates by the
`name` given (or their method). Extensions such as the UI hook in with `ConfigureWebApplication`, `ExtendPipeline` and
`AnnounceUi`.

### Recording received requests

Every request that is not for `/__mock` is recorded, standalone or in a host of your own, so tests can assert on what
the system under test sent:

- **In memory**, always: the last `--record-limit` requests (1000 by default, `0` keeps them all), served by
  `GET /__mock/requests` and cleared by `DELETE /__mock/requests` or `POST /__mock/reset`. This is what
  `MockServerClient.GetRequestsAsync` and the `AssertReceivedAsync` assertions of `Anarchy.Toolkit.Testing.MockServer`
  read, while the server runs.
- **In a file**, with `--record <file>`: one JSON object per line, truncated at startup and never cleared by the control
  API. Read it with `RecordedRequestsFile.Read`, e.g. after the server stopped or from another process. The Anarchy
  `IMockServerBuild` targets record to `artifacts/mock-server/requests.jsonl` by default.

Each record has its sequence, timestamp, method, path, query, headers, body, the expectation (or middleware) that
answered, the status code and the duration.

### Logs

Everything the server writes (startup, requests, warnings, errors, and ASP.NET Core warnings) is also kept in an
in-memory log of `--log-buffer` entries, whatever the console verbosity, so `--quiet` keeps the CLI silent while the
UI or `GET /__mock/logs?after=<sequence>` still follows it.

### Control API

| Endpoint | Description |
| --- | --- |
| `GET /__mock/health` | Ready check. |
| `GET /__mock/requests?method=&path=` | Recorded requests, optionally filtered. |
| `DELETE /__mock/requests` | Forgets recorded requests. |
| `GET/PUT/DELETE /__mock/expectations` | Lists, adds (one or an array) or removes (`?id=`) runtime expectations. |
| `POST /__mock/reset` | Forgets requests and runtime expectations. |
| `POST /__mock/reload` | Reloads expectation files. |
| `GET /__mock/logs?after=` | Log entries, optionally only those after a sequence number. |
| `GET /__mock/middlewares` | The consumer middlewares, in pipeline order. |

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Spectre.Console](https://www.nuget.org/packages/Spectre.Console) | The console banner and request log | Yes: `MockServerConsole` takes an optional `IAnsiConsole` |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
