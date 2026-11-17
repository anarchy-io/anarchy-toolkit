# Anarchy.Toolkit.Web.MockServer

An HTTP mock server for integration and end-to-end tests: JSON expectations, pluggable middlewares, request recording
and colourful console output.

```bash
dotnet add package Anarchy.Toolkit.Web.MockServer
```

- Expectations follow [MockServer](https://www.mock-server.com/)'s JSON shape, so existing `expectations/*.json` files work as they are.
- Your own middlewares run between expectation matching and the answer: they can answer by themselves or change the match.
- Every request can be appended to a JSON-lines file, and is queryable through the `/__mock` control API (see `Anarchy.Toolkit.Testing.MockServer` for test assertions).
- It listens on port `6000`, or the next free one when that is taken.

## When to use it

Use it to stand in for the HTTP services an application calls while integration or end-to-end tests run against it.
It is a console application whose lifetime is usually driven by the Fallout `IMockServerBuild` targets or a Makefile,
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
dotnet Anarchy.Toolkit.Web.MockServer.dll -e "mocks/**/*.json" --record artifacts/mock-requests.jsonl -v detailed
```

| Option | Environment | Description |
| --- | --- | --- |
| `-p, --port` | `MOCKSERVER_PORT` | Port (default `6000` or the next free one; an explicit port that is taken fails). |
| `--host` | `MOCKSERVER_HOST` | Host (default `localhost`). |
| `-e, --expectations` | `MOCKSERVER_EXPECTATIONS` (`;`-separated) | File, directory or glob; repeatable. |
| `-w, --watch` | `MOCKSERVER_WATCH` | Reload expectations when the files change. |
| `-r, --record` | `MOCKSERVER_RECORD` | Append every request to a JSON-lines file. |
| `--record-limit` | `MOCKSERVER_RECORD_LIMIT` | Requests kept in memory (default 1000, `0` = all). |
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
    }));
```

### Control API

| Endpoint | Description |
| --- | --- |
| `GET /__mock/health` | Ready check. |
| `GET /__mock/requests?method=&path=` | Recorded requests, optionally filtered. |
| `DELETE /__mock/requests` | Forgets recorded requests. |
| `GET/PUT/DELETE /__mock/expectations` | Lists, adds (one or an array) or removes (`?id=`) runtime expectations. |
| `POST /__mock/reset` | Forgets requests and runtime expectations. |
| `POST /__mock/reload` | Reloads expectation files. |

## Releases

### 1.0.0

- Initial release.
