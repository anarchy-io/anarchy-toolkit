# Anarchy.Toolkit.Infrastructure.Http

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Reusable HTTP client and repository plumbing for service integrations.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Http
```

- Derive client repositories from `HttpRepositoryBase` or `ScopedHttpRepositoryBase`.
- Configure clients and compose the supplied delegating handlers, such as the timeout handler, as needed. MSAL certificate authentication lives in the opt-in `Anarchy.Toolkit.Infrastructure.Http.Msal` package.
- Keep application-specific endpoints and client behavior in the consuming service.

## When to use it

Use this package in an infrastructure adapter that calls an external HTTP API and needs consistent client configuration, instrumentation or authentication. It is not a generated client: your application owns endpoint routes, payload contracts and retry/idempotency decisions.

## How to use it

1. Define an application-facing interface for the downstream operations and implement it with a repository derived from the appropriate HTTP repository base.
2. Configure an `HttpClient` for the remote service and compose the supplied handlers only when needed, such as timeouts. Supply the host's `AppTelemetry` for instrumentation.
3. Translate downstream responses and errors at the adapter boundary. Configure base addresses, credentials and timeouts per environment rather than embedding them in the repository.

Use mocks for business-rule unit tests and a controlled HTTP endpoint for integration tests that need to verify serialization, authentication and failure handling.

Monitored requests emit execution counts and durations through the injected telemetry. An HTTP response is returned
without checking its status; a non-success status is not a telemetry failure unless the caller treats it as one.

See the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/) and [architecture guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
