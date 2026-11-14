# Anarchy.Toolkit.Infrastructure.Http

Reusable HTTP client and repository plumbing for service integrations.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Http
```

- Derive client repositories from `HttpRepositoryBase` or `ScopedHttpRepositoryBase`.
- Configure clients and compose the supplied delegating handlers, including timeout and MSAL certificate authentication handlers, as needed.
- Keep application-specific endpoints and client behavior in the consuming service.

## When to use it

Use this package in an infrastructure adapter that calls an external HTTP API and needs consistent client configuration, instrumentation or authentication. It is not a generated client: your application owns endpoint routes, payload contracts and retry/idempotency decisions.

## How to use it

1. Define an application-facing interface for the downstream operations and implement it with a repository derived from the appropriate HTTP repository base.
2. Configure an `HttpClient` for the remote service and compose the supplied handlers only when needed, such as timeouts or MSAL certificate authentication. Supply the host's `Observer` for instrumentation.
3. Translate downstream responses and errors at the adapter boundary. Configure base addresses, credentials and timeouts per environment rather than embedding them in the repository.

Use mocks for business-rule unit tests and a controlled HTTP endpoint for integration tests that need to verify serialization, authentication and failure handling.

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme) and [architecture guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md).

## Releases

### 1.0.5

- refactor(src): clean up code style across library packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced HTTP repository bases, client configuration, authentication and timeout handlers, and request and response helpers.
