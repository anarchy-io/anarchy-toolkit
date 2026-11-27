# Anarchy.Toolkit.Runtime.Web.MockServer.Ui

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

A browser UI for [`Anarchy.Toolkit.Runtime.Web.MockServer`](../Runtime.Web.MockServer/README.md), built on
[`Anarchy.Toolkit.Runtime.Web.Ui`](../Runtime.Web.Ui/README.md): see what the mock server answers with and what it received while your
tests or application run.

```bash
dotnet add package Anarchy.Toolkit.Runtime.Web.MockServer.Ui
```

- **Expectations**: every loaded, code and runtime expectation with its method, path, status, priority, remaining
  times and source, as JSON on demand; reloads the expectation files.
- **Requests**: the received requests, live, with what answered them, filterable and viewable as JSON; clears them.
- **Middlewares**: the plugged-in middlewares, in pipeline order.
- **Logs**: a live tail of the server's in-memory log, whatever the console verbosity, filterable by level and text.

## When to use it

When developing against a mock server, or debugging a failing integration test, and reading the console is not enough.
Use `Anarchy.Toolkit.Runtime.Web.MockServer` alone when no one looks at it, e.g. in CI.

## How to use it

Run it standalone, like the plain mock server (same arguments), and open `http://localhost:6001/__mock/ui/`:

```bash
dotnet Anarchy.Toolkit.Runtime.Web.MockServer.Ui.dll -e "mocks/**/*.json"
```

Or call `UseUi()` from a host of your own:

```csharp
return await MockServerEntryPoint.RunAsync(args, server => server
    .AddExpectationsFrom("mocks")
    .UseMiddleware<FakeAuthMiddleware>()
    .UseUi());
```

- The URL is printed in the startup banner. It lives under `/__mock`, so it is never matched against expectations nor
  recorded as a received request.
- `--no-ui` (or `MOCKSERVER_UI=false`) turns it off without changing the host. The Anarchy `IMockServerBuild` targets
  take `--mock-server-ui false`, and the Make targets `UI=false`, e.g. `make mock_server_start UI=false`.
- It has no login: it is meant for local and test environments, like the control API.
- `UseUi(path)` serves it elsewhere under the same rules.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
